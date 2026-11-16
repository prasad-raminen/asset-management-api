using AssetManagement.Domain.Assets.Policies;
using AssetManagement.Domain.Checkouts;
using AssetManagement.Domain.Common;
using AssetManagement.Domain.Exceptions;
using AssetManagement.Domain.Maintenance;

namespace AssetManagement.Domain.Assets;

/// <summary>
/// A tracked piece of equipment. The lifecycle (checkout, return, maintenance, retirement) is
/// enforced here so that no caller can put an asset into an invalid state. Concrete subclasses
/// supply type-specific behaviour such as their maintenance policy and maximum loan period.
/// </summary>
public abstract class Asset : Entity
{
    // Required by EF Core for materialisation.
    protected Asset()
    {
        Name = string.Empty;
        SerialNumber = string.Empty;
        Manufacturer = string.Empty;
        Model = string.Empty;
    }

    protected Asset(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string? location,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        SerialNumber = serialNumber.Trim();
        Name = string.Empty;
        Manufacturer = string.Empty;
        Model = string.Empty;
        UpdateDetails(name, manufacturer, model, location);
        Status = AssetStatus.Available;
        LastServicedAtUtc = utcNow;
    }

    public string Name { get; private set; }

    public string SerialNumber { get; private set; }

    public string Manufacturer { get; private set; }

    public string Model { get; private set; }

    public string? Location { get; private set; }

    public AssetStatus Status { get; private set; }

    /// <summary>When the asset was last serviced (initially, when it was registered).</summary>
    public DateTime LastServicedAtUtc { get; private set; }

    /// <summary>Number of checkouts since the last completed maintenance.</summary>
    public int UsesSinceLastService { get; private set; }

    public abstract AssetType Type { get; }

    /// <summary>The rule deciding when this kind of equipment must be serviced.</summary>
    public abstract IMaintenancePolicy MaintenancePolicy { get; }

    /// <summary>The longest single loan allowed for this kind of equipment.</summary>
    public abstract TimeSpan MaxCheckoutDuration { get; }

    public bool IsMaintenanceDue(DateTime utcNow) =>
        MaintenancePolicy.IsMaintenanceDue(new MaintenanceState(LastServicedAtUtc, UsesSinceLastService), utcNow);

    public void UpdateDetails(string name, string manufacturer, string model, string? location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        EnsureNotRetired();
        Name = name.Trim();
        Manufacturer = manufacturer.Trim();
        Model = model.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
    }

    /// <summary>Lends the asset to a user. Enforces availability, maintenance and loan-period rules.</summary>
    public Checkout CheckOut(Guid userId, DateTime dueAtUtc, DateTime utcNow, string? notes = null)
    {
        switch (Status)
        {
            case AssetStatus.CheckedOut:
                throw DomainErrors.AssetAlreadyCheckedOut(SerialNumber);
            case AssetStatus.UnderMaintenance:
                throw DomainErrors.AssetUnderMaintenance(SerialNumber);
            case AssetStatus.Retired:
                throw DomainErrors.AssetRetired(SerialNumber);
        }

        if (IsMaintenanceDue(utcNow))
        {
            throw DomainErrors.MaintenanceOverdue(SerialNumber, MaintenancePolicy.Description);
        }

        if (dueAtUtc <= utcNow)
        {
            throw DomainErrors.DueDateInPast();
        }

        if (dueAtUtc - utcNow > MaxCheckoutDuration)
        {
            throw new DomainException(
                "checkout.duration_exceeded",
                $"{Type} assets can be checked out for at most {MaxCheckoutDuration.TotalDays:0} days.");
        }

        Status = AssetStatus.CheckedOut;
        UsesSinceLastService++;
        return new Checkout(Id, userId, utcNow, dueAtUtc, notes);
    }

    /// <summary>Returns a checked-out asset and closes the given checkout.</summary>
    public void Return(Checkout checkout, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(checkout);

        if (Status != AssetStatus.CheckedOut)
        {
            throw DomainErrors.AssetNotCheckedOut(SerialNumber);
        }

        if (checkout.AssetId != Id)
        {
            throw DomainErrors.CheckoutMismatch();
        }

        checkout.MarkReturned(utcNow);
        Status = AssetStatus.Available;
    }

    /// <summary>Takes the asset out of circulation for servicing.</summary>
    public MaintenanceRecord StartMaintenance(
        MaintenanceType type,
        string description,
        Guid technicianId,
        DateTime utcNow)
    {
        switch (Status)
        {
            case AssetStatus.CheckedOut:
                throw DomainErrors.AssetCheckedOut(SerialNumber);
            case AssetStatus.UnderMaintenance:
                throw DomainErrors.AssetUnderMaintenance(SerialNumber);
            case AssetStatus.Retired:
                throw DomainErrors.AssetRetired(SerialNumber);
        }

        Status = AssetStatus.UnderMaintenance;
        return new MaintenanceRecord(Id, type, description, technicianId, utcNow);
    }

    /// <summary>Completes servicing, resets the maintenance counters and makes the asset available again.</summary>
    public void CompleteMaintenance(MaintenanceRecord record, DateTime utcNow, decimal? cost, string? resolutionNotes)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (Status != AssetStatus.UnderMaintenance)
        {
            throw DomainErrors.AssetNotUnderMaintenance(SerialNumber);
        }

        if (record.AssetId != Id)
        {
            throw DomainErrors.MaintenanceMismatch();
        }

        record.Complete(utcNow, cost, resolutionNotes);
        Status = AssetStatus.Available;
        LastServicedAtUtc = utcNow;
        UsesSinceLastService = 0;
    }

    /// <summary>
    /// Permanently removes the asset from circulation (soft delete). An open checkout or maintenance
    /// record must be closed first, otherwise it could never be completed once the asset is retired.
    /// </summary>
    public void Retire()
    {
        switch (Status)
        {
            case AssetStatus.CheckedOut:
                throw DomainErrors.AssetCheckedOut(SerialNumber);
            case AssetStatus.UnderMaintenance:
                throw DomainErrors.MaintenanceInProgress(SerialNumber);
        }

        Status = AssetStatus.Retired;
    }

    private void EnsureNotRetired()
    {
        if (Status == AssetStatus.Retired)
        {
            throw DomainErrors.AssetRetired(SerialNumber);
        }
    }
}

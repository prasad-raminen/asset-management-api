namespace AssetManagement.Domain.Exceptions;

/// <summary>Factory for the well-known business rule violations raised by the domain.</summary>
public static class DomainErrors
{
    public static DomainException AssetAlreadyCheckedOut(string serialNumber) =>
        new("asset.already_checked_out", $"Asset '{serialNumber}' is already checked out.");

    public static DomainException AssetUnderMaintenance(string serialNumber) =>
        new("asset.under_maintenance", $"Asset '{serialNumber}' is under maintenance and cannot be checked out.");

    public static DomainException AssetRetired(string serialNumber) =>
        new("asset.retired", $"Asset '{serialNumber}' is retired.");

    public static DomainException MaintenanceOverdue(string serialNumber, string policy) =>
        new("asset.maintenance_overdue",
            $"Asset '{serialNumber}' is overdue for maintenance ({policy}) and cannot be checked out.");

    public static DomainException AssetNotCheckedOut(string serialNumber) =>
        new("asset.not_checked_out", $"Asset '{serialNumber}' is not currently checked out.");

    public static DomainException AssetCheckedOut(string serialNumber) =>
        new("asset.checked_out", $"Asset '{serialNumber}' is checked out and must be returned first.");

    public static DomainException AssetNotUnderMaintenance(string serialNumber) =>
        new("asset.not_under_maintenance", $"Asset '{serialNumber}' is not under maintenance.");

    public static DomainException DueDateInPast() =>
        new("checkout.due_date_in_past", "The due date must be in the future.");

    public static DomainException CheckoutMismatch() =>
        new("checkout.mismatch", "The checkout does not belong to this asset.");

    public static DomainException CheckoutAlreadyReturned() =>
        new("checkout.already_returned", "The checkout has already been returned.");

    public static DomainException MaintenanceAlreadyCompleted() =>
        new("maintenance.already_completed", "The maintenance record has already been completed.");

    public static DomainException MaintenanceMismatch() =>
        new("maintenance.mismatch", "The maintenance record does not belong to this asset.");
}

using AssetManagement.Domain.Assets;
using FluentValidation;

namespace AssetManagement.Application.Assets;

public sealed class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[A-Za-z0-9-_./]+$")
            .WithMessage("Serial number may only contain letters, digits and - _ . /");
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(200);

        // Each asset type requires exactly its own specification field.
        When(x => x.Type == AssetType.LaserScanner, () =>
            RuleFor(x => x.RangeMeters).NotNull().GreaterThan(0).LessThanOrEqualTo(10_000));
        When(x => x.Type == AssetType.Drone, () =>
            RuleFor(x => x.MaxFlightTimeMinutes).NotNull().GreaterThan(0).LessThanOrEqualTo(24 * 60));
        When(x => x.Type == AssetType.LaserTracker, () =>
            RuleFor(x => x.AccuracyMicrometers).NotNull().GreaterThan(0).LessThanOrEqualTo(10_000));

        RuleFor(x => x.RangeMeters).Null()
            .When(x => x.Type != AssetType.LaserScanner)
            .WithMessage("RangeMeters only applies to laser scanners.");
        RuleFor(x => x.MaxFlightTimeMinutes).Null()
            .When(x => x.Type != AssetType.Drone)
            .WithMessage("MaxFlightTimeMinutes only applies to drones.");
        RuleFor(x => x.AccuracyMicrometers).Null()
            .When(x => x.Type != AssetType.LaserTracker)
            .WithMessage("AccuracyMicrometers only applies to laser trackers.");
    }
}

public sealed class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.RangeMeters).GreaterThan(0).LessThanOrEqualTo(10_000);
        RuleFor(x => x.MaxFlightTimeMinutes).GreaterThan(0).LessThanOrEqualTo(24 * 60);
        RuleFor(x => x.AccuracyMicrometers).GreaterThan(0).LessThanOrEqualTo(10_000);
    }
}

public sealed class AssetQueryParametersValidator : AbstractValidator<AssetQueryParameters>
{
    public AssetQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, AssetQueryParameters.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();
        RuleFor(x => x.SortBy)
            .Must(sortBy => AssetQueryParameters.SortableFields.Contains(sortBy!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AssetQueryParameters.SortableFields)}.");
    }
}

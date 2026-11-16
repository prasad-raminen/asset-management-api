using FluentValidation;

namespace AssetManagement.Application.Maintenance;

public sealed class StartMaintenanceRequestValidator : AbstractValidator<StartMaintenanceRequest>
{
    public StartMaintenanceRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CompleteMaintenanceRequestValidator : AbstractValidator<CompleteMaintenanceRequest>
{
    public CompleteMaintenanceRequestValidator()
    {
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.ResolutionNotes).MaximumLength(1000);
    }
}

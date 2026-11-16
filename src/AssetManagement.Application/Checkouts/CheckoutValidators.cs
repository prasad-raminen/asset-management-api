using AssetManagement.Application.Common;
using FluentValidation;

namespace AssetManagement.Application.Checkouts;

public sealed class CheckOutAssetRequestValidator : AbstractValidator<CheckOutAssetRequest>
{
    public CheckOutAssetRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.DueAtUtc)
            .NotEmpty()
            .Must(due => due.AsUtc() > clock.GetUtcNow().UtcDateTime)
            .WithMessage("DueAtUtc must be in the future.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

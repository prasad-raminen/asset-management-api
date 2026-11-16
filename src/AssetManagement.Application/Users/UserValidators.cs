using FluentValidation;

namespace AssetManagement.Application.Users;

public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}

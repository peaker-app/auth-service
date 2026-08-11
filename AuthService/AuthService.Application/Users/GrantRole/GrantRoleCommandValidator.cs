using FluentValidation;

namespace AuthService.Application.Users.GrantRole;

internal sealed class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Role).IsInEnum();
    }
}

using FluentValidation;

namespace AuthService.Application.Users.RevokeRole;

internal sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Role).IsInEnum();
    }
}

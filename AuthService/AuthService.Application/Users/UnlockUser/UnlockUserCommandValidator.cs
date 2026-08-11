using FluentValidation;

namespace AuthService.Application.Users.UnlockUser;

internal sealed class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}

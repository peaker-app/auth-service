using FluentValidation;

namespace AuthService.Application.Users.LockUser;

internal sealed class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.UserId)
            .NotEqual(command => command.ActorId)
            .WithMessage("Un administrador no puede bloquear su propia cuenta.");
    }
}

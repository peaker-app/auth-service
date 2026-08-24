using FluentValidation;

namespace AuthService.Application.Users.LogoutUser;

internal sealed class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}

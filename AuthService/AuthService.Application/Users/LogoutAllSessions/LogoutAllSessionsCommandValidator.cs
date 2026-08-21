using FluentValidation;

namespace AuthService.Application.Users.LogoutAllSessions;

internal sealed class LogoutAllSessionsCommandValidator : AbstractValidator<LogoutAllSessionsCommand>
{
    public LogoutAllSessionsCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

using FluentValidation;

namespace AuthService.Application.PasswordResets.IssuePasswordReset;

internal sealed class IssuePasswordResetCommandValidator : AbstractValidator<IssuePasswordResetCommand>
{
    public IssuePasswordResetCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

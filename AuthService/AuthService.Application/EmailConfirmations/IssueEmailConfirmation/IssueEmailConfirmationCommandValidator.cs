using FluentValidation;

namespace AuthService.Application.EmailConfirmations.IssueEmailConfirmation;

internal sealed class IssueEmailConfirmationCommandValidator : AbstractValidator<IssueEmailConfirmationCommand>
{
    public IssueEmailConfirmationCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

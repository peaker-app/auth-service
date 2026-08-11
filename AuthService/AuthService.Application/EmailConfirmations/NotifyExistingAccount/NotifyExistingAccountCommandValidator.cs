using FluentValidation;

namespace AuthService.Application.EmailConfirmations.NotifyExistingAccount;

internal sealed class NotifyExistingAccountCommandValidator : AbstractValidator<NotifyExistingAccountCommand>
{
    public NotifyExistingAccountCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

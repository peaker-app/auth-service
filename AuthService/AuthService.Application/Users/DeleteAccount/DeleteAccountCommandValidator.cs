using FluentValidation;

namespace AuthService.Application.Users.DeleteAccount;

internal sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

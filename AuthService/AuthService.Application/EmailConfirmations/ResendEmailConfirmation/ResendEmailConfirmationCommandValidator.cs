using FluentValidation;

namespace AuthService.Application.EmailConfirmations.ResendEmailConfirmation;

internal sealed class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator() => RuleFor(command => command.UserId).NotEmpty();
}

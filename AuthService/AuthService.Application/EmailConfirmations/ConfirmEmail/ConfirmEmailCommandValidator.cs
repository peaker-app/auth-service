using FluentValidation;

namespace AuthService.Application.EmailConfirmations.ConfirmEmail;

internal sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    private const int MaxTokenLength = 200;

    public ConfirmEmailCommandValidator() =>
        RuleFor(command => command.Token).NotEmpty().MaximumLength(MaxTokenLength);
}

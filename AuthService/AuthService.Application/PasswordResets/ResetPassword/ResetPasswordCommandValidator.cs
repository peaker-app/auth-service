using AuthService.Application.Users.RegisterUser;
using FluentValidation;

namespace AuthService.Application.PasswordResets.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    private const int MaxTokenLength = 200;

    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(MaxTokenLength);

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(RegisterUserCommandValidator.MinimumPasswordLength);
    }
}

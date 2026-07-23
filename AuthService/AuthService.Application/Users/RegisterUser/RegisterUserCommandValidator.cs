using FluentValidation;

namespace AuthService.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public const int MinimumPasswordLength = 10;

    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty();

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(MinimumPasswordLength);
    }
}

using AuthService.Domain.Users;
using FluentValidation;

namespace AuthService.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public const int MinimumPasswordLength = 10;

    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty();

        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(Username.MinLength)
            .MaximumLength(Username.MaxLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(MinimumPasswordLength);

        RuleFor(command => command.AcceptedTerms)
            .Equal(true)
            .WithMessage("Hay que aceptar las condiciones de uso para crear la cuenta.");
    }
}

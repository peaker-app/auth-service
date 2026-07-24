using FluentValidation;

namespace AuthService.Application.Users.LoginUser;

internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Identifier).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
    }
}

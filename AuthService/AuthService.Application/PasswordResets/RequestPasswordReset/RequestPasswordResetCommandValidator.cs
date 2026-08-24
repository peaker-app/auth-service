using AuthService.Domain.Users;
using FluentValidation;

namespace AuthService.Application.PasswordResets.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator() =>
        RuleFor(command => command.Email).NotEmpty().MaximumLength(Email.MaxLength);
}

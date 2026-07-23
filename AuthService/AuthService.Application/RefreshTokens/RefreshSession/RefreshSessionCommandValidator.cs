using FluentValidation;

namespace AuthService.Application.RefreshTokens.RefreshSession;

internal sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}

using Common.Domain.Results;

namespace AuthService.Domain.RefreshTokens;

public static class RefreshTokenErrors
{
    public static readonly Error InvalidOrExpired =
        Error.Unauthorized("RefreshToken.InvalidOrExpired", "El token de renovación no es válido o ha expirado.");
}

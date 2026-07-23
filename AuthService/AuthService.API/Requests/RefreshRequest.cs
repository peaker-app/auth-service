using AuthService.Application.RefreshTokens.RefreshSession;

namespace AuthService.API.Requests;

public sealed record RefreshRequest(string RefreshToken)
{
    public RefreshSessionCommand ToCommand(string? ipAddress) => new(RefreshToken, ipAddress);
}

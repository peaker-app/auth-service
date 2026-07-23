namespace AuthService.Application.Authentication;

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType);

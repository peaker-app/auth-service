using AuthService.Application.Abstractions;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;

namespace AuthService.Application.Authentication;

internal sealed record IssuedTokens(AuthTokensResponse Response, RefreshToken RefreshToken);

internal interface IAuthTokenIssuer
{
    IssuedTokens Issue(User user, DateTime utcNow, string? ipAddress);
}

internal sealed class AuthTokenIssuer(
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshTokenRepository refreshTokenRepository) : IAuthTokenIssuer
{
    private const string BearerTokenType = "Bearer";

    public IssuedTokens Issue(User user, DateTime utcNow, string? ipAddress)
    {
        GeneratedAccessToken access = accessTokenGenerator.Generate(user);
        GeneratedRefreshToken refresh = refreshTokenGenerator.Generate(utcNow);

        RefreshToken refreshToken = RefreshToken.Issue(
            new RefreshTokenDraft(user.Id, refresh.TokenHash, refresh.ExpiresAtUtc, ipAddress));

        refreshTokenRepository.Add(refreshToken);

        AuthTokensResponse response = new(access.Value, refresh.RawToken, access.ExpiresInSeconds, BearerTokenType);

        return new IssuedTokens(response, refreshToken);
    }
}

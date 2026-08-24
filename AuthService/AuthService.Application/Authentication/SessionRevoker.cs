using AuthService.Application.Abstractions;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;

namespace AuthService.Application.Authentication;

internal interface ISessionRevoker
{
    Task RevokeAllAsync(User user, CancellationToken cancellationToken);
}

internal sealed class SessionRevoker(
    IRefreshTokenRepository refreshTokenRepository,
    ILoginThrottle loginThrottle,
    IDateTimeProvider dateTimeProvider) : ISessionRevoker
{
    public async Task RevokeAllAsync(User user, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RefreshToken> activeTokens =
            await refreshTokenRepository.GetActiveByUserAsync(user.Id, cancellationToken);

        DateTime utcNow = dateTimeProvider.UtcNow;

        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await loginThrottle.ClearAsync(user.Email.Value, cancellationToken);
        await loginThrottle.ClearAsync(user.Username.Value, cancellationToken);
    }
}

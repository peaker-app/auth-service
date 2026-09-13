using AuthService.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<bool> IsActiveAsync(Guid refreshTokenId, DateTime utcNow, CancellationToken cancellationToken) =>
        context.RefreshTokens.AnyAsync(
            token => token.Id == refreshTokenId && token.RevokedAtUtc == null && token.ExpiresAtUtc > utcNow,
            cancellationToken);

    public async Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null && token.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => context.RefreshTokens.Add(refreshToken);
}

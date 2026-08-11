using AuthService.Domain.PasswordResets;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository(AuthDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.PasswordResetTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await context.PasswordResetTokens
            .Where(token => token.UserId == userId && token.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

    public void Add(PasswordResetToken token) => context.PasswordResetTokens.Add(token);
}

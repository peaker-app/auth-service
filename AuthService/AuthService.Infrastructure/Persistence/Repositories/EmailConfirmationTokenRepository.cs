using AuthService.Domain.EmailConfirmations;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

internal sealed class EmailConfirmationTokenRepository(AuthDbContext context) : IEmailConfirmationTokenRepository
{
    public Task<EmailConfirmationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.EmailConfirmationTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<EmailConfirmationToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await context.EmailConfirmationTokens
            .Where(token => token.UserId == userId && token.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

    public Task<EmailConfirmationToken?> GetLatestByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        context.EmailConfirmationTokens
            .Where(token => token.UserId == userId)
            .OrderByDescending(token => token.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(EmailConfirmationToken token) => context.EmailConfirmationTokens.Add(token);
}

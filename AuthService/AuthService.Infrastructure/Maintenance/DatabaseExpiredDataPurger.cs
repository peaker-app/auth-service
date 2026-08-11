using AuthService.Application.Abstractions;
using AuthService.Application.Maintenance.PurgeExpiredData;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Maintenance;

internal sealed class DatabaseExpiredDataPurger(
    AuthDbContext context,
    IOptions<DataRetentionOptions> options) : IExpiredDataPurger
{
    public async Task<DataPurgeResponse> PurgeAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        DataRetentionOptions retention = options.Value;

        return new DataPurgeResponse(
            await PurgeLoginAttemptsAsync(utcNow - retention.LoginAttempts, cancellationToken),
            await PurgeEmailDispatchesAsync(utcNow - retention.EmailDispatches, cancellationToken),
            await PurgeClosedSessionsAsync(utcNow - retention.ClosedSessions, cancellationToken),
            await PurgeConfirmationTokensAsync(utcNow - retention.SpentTokens, cancellationToken),
            await PurgeResetTokensAsync(utcNow - retention.SpentTokens, cancellationToken));
    }

    private Task<int> PurgeLoginAttemptsAsync(DateTime before, CancellationToken cancellationToken) =>
        context.Set<LoginAttempt>()
            .Where(attempt => attempt.AttemptedAtUtc < before)
            .ExecuteDeleteAsync(cancellationToken);

    private Task<int> PurgeEmailDispatchesAsync(DateTime before, CancellationToken cancellationToken) =>
        context.Set<EmailDispatch>()
            .Where(dispatch => dispatch.SentAtUtc < before)
            .ExecuteDeleteAsync(cancellationToken);

    private Task<int> PurgeClosedSessionsAsync(DateTime before, CancellationToken cancellationToken) =>
        context.RefreshTokens
            .Where(token => token.ExpiresAtUtc < before || (token.RevokedAtUtc != null && token.RevokedAtUtc < before))
            .ExecuteDeleteAsync(cancellationToken);

    private Task<int> PurgeConfirmationTokensAsync(DateTime before, CancellationToken cancellationToken) =>
        context.EmailConfirmationTokens
            .Where(token => token.ExpiresAtUtc < before || (token.ConsumedAtUtc != null && token.ConsumedAtUtc < before))
            .ExecuteDeleteAsync(cancellationToken);

    private Task<int> PurgeResetTokensAsync(DateTime before, CancellationToken cancellationToken) =>
        context.PasswordResetTokens
            .Where(token => token.ExpiresAtUtc < before || (token.ConsumedAtUtc != null && token.ConsumedAtUtc < before))
            .ExecuteDeleteAsync(cancellationToken);
}

using AuthService.Application.Abstractions;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets;

internal interface IPasswordResetIssuer
{
    Task<Result> IssueAsync(User user, CancellationToken cancellationToken);
}

internal sealed class PasswordResetIssuer(
    IPasswordResetTokenRepository tokenRepository,
    IPasswordResetTokenGenerator tokenGenerator,
    IPasswordResetEmailSender emailSender,
    IEmailQuota emailQuota,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IPasswordResetIssuer
{
    public async Task<Result> IssueAsync(User user, CancellationToken cancellationToken)
    {
        EmailQuotaVerdict verdict = await emailQuota.TryReserveAsync(user.Email.Value, cancellationToken);

        if (verdict is not EmailQuotaVerdict.Allowed)
        {
            return Result.Failure(PasswordResetQuotaErrors.For(verdict));
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        await InvalidateActiveTokensAsync(user.Id, utcNow, cancellationToken);

        GeneratedPasswordResetToken generated = tokenGenerator.Generate(utcNow);

        tokenRepository.Add(PasswordResetToken.Issue(new PasswordResetTokenDraft(
            user.Id,
            generated.TokenHash,
            utcNow,
            generated.ExpiresAtUtc)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        bool delivered = await emailSender.SendAsync(user.Email.Value, generated.RawToken, cancellationToken);

        return delivered ? Result.Success() : Result.Failure(PasswordResetErrors.DeliveryFailed);
    }

    private async Task InvalidateActiveTokensAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PasswordResetToken> active =
            await tokenRepository.GetActiveByUserAsync(userId, cancellationToken);

        foreach (PasswordResetToken token in active)
        {
            token.Invalidate(utcNow);
        }
    }
}

using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;

namespace AuthService.Application.EmailConfirmations;

internal interface IEmailConfirmationIssuer
{
    Task<Result> IssueAsync(User user, CancellationToken cancellationToken);
}

internal sealed class EmailConfirmationIssuer(
    IEmailConfirmationTokenRepository tokenRepository,
    IEmailConfirmationTokenGenerator tokenGenerator,
    IConfirmationEmailSender confirmationEmailSender,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IEmailConfirmationIssuer
{
    public async Task<Result> IssueAsync(User user, CancellationToken cancellationToken)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        await InvalidateActiveTokensAsync(user.Id, utcNow, cancellationToken);

        GeneratedEmailConfirmationToken generated = tokenGenerator.Generate(utcNow);

        tokenRepository.Add(EmailConfirmationToken.Issue(new EmailConfirmationTokenDraft(
            user.Id,
            generated.TokenHash,
            utcNow,
            generated.ExpiresAtUtc)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        bool delivered = await confirmationEmailSender.SendAsync(
            user.Email.Value, generated.RawToken, cancellationToken);

        return delivered ? Result.Success() : Result.Failure(EmailConfirmationErrors.DeliveryFailed);
    }

    private async Task InvalidateActiveTokensAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EmailConfirmationToken> active =
            await tokenRepository.GetActiveByUserAsync(userId, cancellationToken);

        foreach (EmailConfirmationToken token in active)
        {
            token.Invalidate(utcNow);
        }
    }
}

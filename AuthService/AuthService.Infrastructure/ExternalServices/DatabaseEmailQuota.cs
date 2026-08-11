using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Infrastructure.Persistence;
using Common.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class DatabaseEmailQuota(
    AuthDbContext context,
    IOptions<EmailQuotaOptions> options,
    IDateTimeProvider dateTimeProvider) : IEmailQuota
{
    public async Task<EmailQuotaVerdict> TryReserveAsync(
        string recipientEmail,
        CancellationToken cancellationToken)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        string recipientHash = SubjectHash.Of(recipientEmail);

        EmailQuotaVerdict verdict = await EvaluateAsync(recipientHash, utcNow, cancellationToken);

        if (verdict is not EmailQuotaVerdict.Allowed)
        {
            return verdict;
        }

        context.Set<EmailDispatch>().Add(new EmailDispatch
        {
            Id = Guid.CreateVersion7(),
            RecipientHash = recipientHash,
            SentAtUtc = utcNow
        });

        await context.SaveChangesAsync(cancellationToken);

        return EmailQuotaVerdict.Allowed;
    }

    private async Task<EmailQuotaVerdict> EvaluateAsync(
        string recipientHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        EmailQuotaOptions limits = options.Value;
        DateTime lastHour = utcNow.AddHours(-1);
        DateTime lastDay = utcNow.AddDays(-1);

        IQueryable<EmailDispatch> log = context.Set<EmailDispatch>().AsNoTracking();

        if (await log.CountAsync(entry => entry.SentAtUtc >= lastHour, cancellationToken) >= limits.GlobalPerHour)
        {
            return EmailQuotaVerdict.GlobalExhausted;
        }

        IQueryable<EmailDispatch> forRecipient = log.Where(entry => entry.RecipientHash == recipientHash);

        int inLastHour = await forRecipient.CountAsync(entry => entry.SentAtUtc >= lastHour, cancellationToken);
        int inLastDay = await forRecipient.CountAsync(entry => entry.SentAtUtc >= lastDay, cancellationToken);

        return inLastHour >= limits.PerRecipientPerHour || inLastDay >= limits.PerRecipientPerDay
            ? EmailQuotaVerdict.RecipientExhausted
            : EmailQuotaVerdict.Allowed;
    }
}

using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.EmailConfirmations;

public sealed class EmailConfirmationToken : AggregateRoot
{
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(2);

    private EmailConfirmationToken()
    {
    }

    private EmailConfirmationToken(Guid id, EmailConfirmationTokenDraft draft) : base(id)
    {
        UserId = draft.UserId;
        TokenHash = draft.TokenHash;
        IssuedAtUtc = draft.IssuedAtUtc;
        ExpiresAtUtc = draft.ExpiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    public static EmailConfirmationToken Issue(EmailConfirmationTokenDraft draft) =>
        new(Guid.CreateVersion7(), draft);

    public bool IsActive(DateTime utcNow) => ConsumedAtUtc is null && ExpiresAtUtc > utcNow;

    public bool AllowsReissue(DateTime utcNow) => utcNow - IssuedAtUtc >= ResendCooldown;

    public Result Consume(DateTime utcNow)
    {
        if (!IsActive(utcNow))
        {
            return Result.Failure(EmailConfirmationErrors.InvalidOrExpired);
        }

        ConsumedAtUtc = utcNow;

        return Result.Success();
    }

    public void Invalidate(DateTime utcNow)
    {
        if (ConsumedAtUtc is null)
        {
            ConsumedAtUtc = utcNow;
        }
    }
}

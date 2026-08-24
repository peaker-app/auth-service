using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.PasswordResets;

public sealed class PasswordResetToken : AggregateRoot
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid id, PasswordResetTokenDraft draft) : base(id)
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

    public static PasswordResetToken Issue(PasswordResetTokenDraft draft) =>
        new(Guid.CreateVersion7(), draft);

    public bool IsActive(DateTime utcNow) => ConsumedAtUtc is null && ExpiresAtUtc > utcNow;

    public Result Consume(DateTime utcNow)
    {
        if (!IsActive(utcNow))
        {
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);
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

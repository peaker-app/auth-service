using Common.Domain.Abstractions;

namespace AuthService.Domain.RefreshTokens;

public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, RefreshTokenDraft draft) : base(id)
    {
        UserId = draft.UserId;
        TokenHash = draft.TokenHash;
        ExpiresAtUtc = draft.ExpiresAtUtc;
        CreatedByIp = draft.CreatedByIp;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public string? CreatedByIp { get; private set; }

    public static RefreshToken Issue(RefreshTokenDraft draft) => new(Guid.CreateVersion7(), draft);

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTime utcNow, Guid? replacedById = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        ReplacedById = replacedById;
    }
}

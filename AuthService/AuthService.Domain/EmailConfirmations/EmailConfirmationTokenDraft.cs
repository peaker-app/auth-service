namespace AuthService.Domain.EmailConfirmations;

public sealed record EmailConfirmationTokenDraft(
    Guid UserId,
    string TokenHash,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);

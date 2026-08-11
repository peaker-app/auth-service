namespace AuthService.Domain.PasswordResets;

public sealed record PasswordResetTokenDraft(
    Guid UserId,
    string TokenHash,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);

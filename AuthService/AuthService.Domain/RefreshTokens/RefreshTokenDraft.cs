namespace AuthService.Domain.RefreshTokens;

public sealed record RefreshTokenDraft(Guid UserId, string TokenHash, DateTime ExpiresAtUtc, string? CreatedByIp);

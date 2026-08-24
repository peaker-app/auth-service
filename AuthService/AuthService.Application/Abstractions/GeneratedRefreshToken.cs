namespace AuthService.Application.Abstractions;

public sealed record GeneratedRefreshToken(string RawToken, string TokenHash, DateTime ExpiresAtUtc);

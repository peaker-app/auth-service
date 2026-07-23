namespace AuthService.Application.Abstractions;

public sealed record GeneratedAccessToken(string Value, DateTime ExpiresAtUtc, int ExpiresInSeconds);

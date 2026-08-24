namespace AuthService.Application.Abstractions;

public sealed record GeneratedEmailConfirmationToken(string RawToken, string TokenHash, DateTime ExpiresAtUtc);

namespace AuthService.Application.Abstractions;

public sealed record GeneratedPasswordResetToken(string RawToken, string TokenHash, DateTime ExpiresAtUtc);

public interface IPasswordResetTokenGenerator
{
    GeneratedPasswordResetToken Generate(DateTime utcNow);

    string Hash(string rawToken);
}

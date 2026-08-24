using AuthService.Domain.RefreshTokens;

namespace AuthService.Domain.UnitTests.TestData;

internal static class RefreshTokenMother
{
    public const string TokenHash = "sha256-token-hash";

    public static RefreshToken Active(DateTime utcNow) =>
        RefreshToken.Issue(new RefreshTokenDraft(Guid.CreateVersion7(), TokenHash, utcNow.AddDays(7), "127.0.0.1"));
}

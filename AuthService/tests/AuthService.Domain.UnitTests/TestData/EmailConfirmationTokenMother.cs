using AuthService.Domain.EmailConfirmations;

namespace AuthService.Domain.UnitTests.TestData;

internal static class EmailConfirmationTokenMother
{
    public const string TokenHash = "sha256-confirmation-hash";

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public static EmailConfirmationToken Active(DateTime utcNow) => Active(Guid.CreateVersion7(), utcNow);

    public static EmailConfirmationToken Active(Guid userId, DateTime utcNow) =>
        EmailConfirmationToken.Issue(new EmailConfirmationTokenDraft(
            userId,
            TokenHash,
            utcNow,
            utcNow.Add(Lifetime)));
}

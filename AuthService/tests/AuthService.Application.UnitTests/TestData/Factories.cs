using AuthService.Application.Authentication;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;

namespace AuthService.Application.UnitTests.TestData;

internal static class Factories
{
    public const string DefaultEmail = "hiker@peaker.io";
    public const string DefaultUsername = "hiker";
    public const string DefaultHash = "argon2id$hash";

    public static User ActiveUser() => User.Register(
        Email.Create(DefaultEmail).Value,
        Username.Create(DefaultUsername).Value,
        DefaultHash).Value;

    public static User LockedUser(DateTime utcNow)
    {
        User user = ActiveUser();

        for (int attempt = 0; attempt < User.MaxFailedAttempts; attempt++)
        {
            user.RecordFailedLogin(utcNow);
        }

        return user;
    }

    public static User ConfirmedUser()
    {
        User user = ActiveUser();
        user.ConfirmEmail();

        return user;
    }

    public static User DeletedUser()
    {
        User user = ActiveUser();
        user.Delete();

        return user;
    }

    public static RefreshToken RefreshTokenFor(Guid userId, DateTime utcNow) =>
        RefreshToken.Issue(new RefreshTokenDraft(userId, "token-hash", utcNow.AddDays(7), "127.0.0.1"));

    public static IssuedTokens IssuedFor(RefreshToken refreshToken) =>
        new(new AuthTokensResponse("access-token", "raw-refresh-token", 900, "Bearer"), refreshToken);

    public static EmailConfirmationToken ConfirmationTokenFor(Guid userId, DateTime issuedAtUtc) =>
        EmailConfirmationToken.Issue(new EmailConfirmationTokenDraft(
            userId,
            "confirmation-hash",
            issuedAtUtc,
            issuedAtUtc.AddHours(24)));
}

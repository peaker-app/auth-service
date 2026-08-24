using AuthService.Application.Authentication;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;

namespace AuthService.Application.UnitTests.TestData;

internal static class Factories
{
    public const string DefaultEmail = "hiker@peaker.io";
    public const string DefaultUsername = "hiker";
    public const string DefaultHash = "argon2id$hash";
    public const string PseudonymizedEmail = "deleted+0123456789abcdef@peaker.invalid";

    public const string TermsVersion = "2026-08-11";

    public static readonly DateTime TermsAcceptedAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    public static User ActiveUser() => User.Register(new UserDraft(
        Email.Create(DefaultEmail).Value,
        Username.Create(DefaultUsername).Value,
        DefaultHash,
        TermsAcceptance.Of(TermsVersion, TermsAcceptedAt))).Value;

    public static User LockedUser()
    {
        User user = ActiveUser();
        user.Lock();

        return user;
    }

    public static User AdminUser()
    {
        User user = ActiveUser();
        user.Grant(UserRole.Admin);

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
        user.Delete(Pseudonym());

        return user;
    }

    public static Email Pseudonym() => Email.Create(PseudonymizedEmail).Value;

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

    public static PasswordResetToken ResetTokenFor(Guid userId, DateTime issuedAtUtc) =>
        PasswordResetToken.Issue(new PasswordResetTokenDraft(
            userId,
            "reset-hash",
            issuedAtUtc,
            issuedAtUtc.AddHours(1)));
}

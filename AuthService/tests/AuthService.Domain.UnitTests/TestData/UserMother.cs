using AuthService.Domain.Users;

namespace AuthService.Domain.UnitTests.TestData;

internal static class UserMother
{
    public const string PasswordHash = "argon2id$hash";

    public const string TermsVersion = "2026-08-11";

    public static readonly DateTime AcceptedAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    public static User Registered() => User.Register(Draft()).Value;

    public static UserDraft Draft(string passwordHash = PasswordHash) => new(
        TestEmail.Create(),
        TestUsername.Create(),
        passwordHash,
        TermsAcceptance.Of(TermsVersion, AcceptedAt));

    public static User Confirmed()
    {
        User user = Registered();
        user.ConfirmEmail();

        return user;
    }

    public static User Deleted()
    {
        User user = Registered();
        user.Delete(TestEmail.Pseudonym());

        return user;
    }

    public static User Admin()
    {
        User user = Registered();
        user.Grant(UserRole.Admin);

        return user;
    }

    public static User Locked()
    {
        User user = Registered();
        user.Lock();

        return user;
    }
}

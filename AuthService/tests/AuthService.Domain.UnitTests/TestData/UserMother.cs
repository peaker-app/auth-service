using AuthService.Domain.Users;

namespace AuthService.Domain.UnitTests.TestData;

internal static class UserMother
{
    public const string PasswordHash = "argon2id$hash";

    public static User Registered() => User.Register(TestEmail.Create(), PasswordHash).Value;

    public static User WithFailedLogins(int count, DateTime utcNow)
    {
        User user = Registered();

        for (int attempt = 0; attempt < count; attempt++)
        {
            user.RecordFailedLogin(utcNow);
        }

        return user;
    }
}

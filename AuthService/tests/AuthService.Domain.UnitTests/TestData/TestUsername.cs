using AuthService.Domain.Users;

namespace AuthService.Domain.UnitTests.TestData;

internal static class TestUsername
{
    public const string Raw = "Hiker_Ruben";

    public const string Normalized = "hiker_ruben";

    public static Username Create() => Username.Create(Raw).Value;
}

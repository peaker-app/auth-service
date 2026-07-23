using AuthService.Domain.Users;

namespace AuthService.Domain.UnitTests.TestData;

internal static class TestEmail
{
    public const string Raw = "Hiker@Peaker.IO";

    public const string Normalized = "hiker@peaker.io";

    public static Email Create() => Email.Create(Raw).Value;
}

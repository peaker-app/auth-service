using AuthService.Domain.Users;

namespace AuthService.Domain.UnitTests.TestData;

internal static class TestEmail
{
    public const string Raw = "Hiker@Peaker.IO";

    public const string Normalized = "hiker@peaker.io";

    public const string PseudonymValue = "deleted+0123456789abcdef@peaker.invalid";

    public static Email Create() => Email.Create(Raw).Value;

    public static Email Pseudonym() => Email.Create(PseudonymValue).Value;
}

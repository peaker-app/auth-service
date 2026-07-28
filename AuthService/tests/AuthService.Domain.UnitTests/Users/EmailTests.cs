using AuthService.Domain.Users;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class EmailTests
{
    [Theory]
    [InlineData("Hiker@Peaker.IO", "hiker@peaker.io")]
    [InlineData("  USER@Test.Com  ", "user@test.com")]
    public void Create_WithValidEmail_NormalizesToLowercaseAndTrimmed(string raw, string expected)
    {
        Result<Email> result = Email.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_WithNullOrWhitespace_ReturnsEmailEmpty()
    {
        Result<Email> result = Email.Create("   ");

        result.Error.Should().Be(UserErrors.EmailEmpty);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@no-local.com")]
    public void Create_WithInvalidFormat_ReturnsEmailInvalid(string raw)
    {
        Result<Email> result = Email.Create(raw);

        result.Error.Should().Be(UserErrors.EmailInvalid);
    }

    [Fact]
    public void Create_ExceedingMaxLength_ReturnsEmailTooLong()
    {
        string raw = new string('a', Email.MaxLength) + "@peaker.io";

        Result<Email> result = Email.Create(raw);

        result.Error.Should().Be(UserErrors.EmailTooLong);
    }
}

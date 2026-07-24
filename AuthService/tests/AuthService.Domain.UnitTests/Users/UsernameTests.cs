using AuthService.Domain.Users;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class UsernameTests
{
    [Theory]
    [InlineData("ruben")]
    [InlineData("Hiker_Ruben")]
    [InlineData("ruben.fernandez")]
    [InlineData("ruben-fer")]
    [InlineData("r2d2")]
    public void Create_WithValidFormat_Succeeds(string raw)
    {
        Result<Username> result = Username.Create(raw);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsButKeepsCasing()
    {
        Result<Username> result = Username.Create("  Hiker_Ruben  ");

        result.Value.Value.Should().Be("Hiker_Ruben");
    }

    [Fact]
    public void Create_NormalizesToLowercaseForUniqueness()
    {
        Result<Username> result = Username.Create("Hiker_Ruben");

        result.Value.NormalizedValue.Should().Be("hiker_ruben");
    }

    [Fact]
    public void Equals_IgnoringCase_ConsidersUsernamesEqual()
    {
        Username lowercase = Username.Create("ruben").Value;
        Username uppercase = Username.Create("RUBEN").Value;

        lowercase.Should().Be(uppercase);
    }

    [Fact]
    public void Create_WithNullOrWhitespace_ReturnsUsernameEmpty()
    {
        Result<Username> result = Username.Create("   ");

        result.Error.Should().Be(UserErrors.UsernameEmpty);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("_ruben")]
    [InlineData("ruben_")]
    [InlineData("ruben__fer")]
    [InlineData("ruben fer")]
    [InlineData("rubén")]
    [InlineData("ruben@peaker")]
    public void Create_WithInvalidFormat_ReturnsUsernameInvalid(string raw)
    {
        Result<Username> result = Username.Create(raw);

        result.Error.Should().Be(UserErrors.UsernameInvalid);
    }

    [Fact]
    public void Create_ExceedingMaxLength_ReturnsUsernameInvalid()
    {
        Result<Username> result = Username.Create(new string('a', Username.MaxLength + 1));

        result.Error.Should().Be(UserErrors.UsernameInvalid);
    }
}

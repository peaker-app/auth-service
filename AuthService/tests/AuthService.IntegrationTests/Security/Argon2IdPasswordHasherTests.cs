using System.Diagnostics;
using AuthService.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Security;

public sealed class Argon2IdPasswordHasherTests
{
    private const string Password = "correct-horse-battery";
    private const double MinimumTimingRatio = 0.25;

    private readonly Argon2IdPasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithTheMatchingHash_ReturnsTrue() =>
        _hasher.Verify(Password, _hasher.Hash(Password)).Should().BeTrue();

    [Fact]
    public void Verify_WithAnotherPassword_ReturnsFalse() =>
        _hasher.Verify("another-password", _hasher.Hash(Password)).Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Verify_WithoutAStoredHash_ReturnsFalse(string? passwordHash) =>
        _hasher.Verify(Password, passwordHash).Should().BeFalse();

    [Fact]
    public void Verify_WithoutAStoredHash_CostsTheSameOrderOfMagnitudeAsARealVerification()
    {
        string storedHash = _hasher.Hash(Password);
        _ = _hasher.Verify(Password, null);

        TimeSpan real = Measure(() => _hasher.Verify(Password, storedHash));
        TimeSpan missing = Measure(() => _hasher.Verify(Password, null));

        missing.Should().BeGreaterThan(real * MinimumTimingRatio);
    }

    private static TimeSpan Measure(Action verification)
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        verification();

        return Stopwatch.GetElapsedTime(startTimestamp);
    }
}

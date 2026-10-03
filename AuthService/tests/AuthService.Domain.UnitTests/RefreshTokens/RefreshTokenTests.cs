using AuthService.Domain.RefreshTokens;
using AuthService.Domain.UnitTests.TestData;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.RefreshTokens;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Leeway = TimeSpan.FromSeconds(30);

    [Fact]
    public void Issue_SetsProvidedValuesAndLeavesTokenActive()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);

        token.TokenHash.Should().Be(RefreshTokenMother.TokenHash);
        token.RevokedAtUtc.Should().BeNull();
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);

        token.IsActive(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevocationAndReplacement()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);
        Guid replacement = Guid.CreateVersion7();

        token.Revoke(Now, replacement);

        token.RevokedAtUtc.Should().Be(Now);
        token.ReplacedById.Should().Be(replacement);
        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_KeepsOriginalRevocation()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);
        token.Revoke(Now);

        token.Revoke(Now.AddDays(1), Guid.CreateVersion7());

        token.RevokedAtUtc.Should().Be(Now);
        token.ReplacedById.Should().BeNull();
    }

    [Fact]
    public void ReplacementIdWithinLeeway_WhenStillActive_ReturnsNull()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);

        token.ReplacementIdWithinLeeway(Now, Leeway).Should().BeNull();
    }

    [Fact]
    public void ReplacementIdWithinLeeway_WhenRotatedInsideTheWindow_ReturnsTheReplacement()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);
        Guid replacement = Guid.CreateVersion7();
        token.Revoke(Now, replacement);

        token.ReplacementIdWithinLeeway(Now.AddSeconds(20), Leeway).Should().Be(replacement);
    }

    [Fact]
    public void ReplacementIdWithinLeeway_WhenRotatedBeforeTheWindow_ReturnsNull()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);
        token.Revoke(Now, Guid.CreateVersion7());

        token.ReplacementIdWithinLeeway(Now.AddMinutes(5), Leeway).Should().BeNull();
    }

    [Fact]
    public void ReplacementIdWithinLeeway_WhenRevokedWithoutReplacement_ReturnsNull()
    {
        RefreshToken token = RefreshTokenMother.Active(Now);
        token.Revoke(Now);

        token.ReplacementIdWithinLeeway(Now, Leeway).Should().BeNull();
    }
}

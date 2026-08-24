using AuthService.Domain.RefreshTokens;
using AuthService.Domain.UnitTests.TestData;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.RefreshTokens;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

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
}

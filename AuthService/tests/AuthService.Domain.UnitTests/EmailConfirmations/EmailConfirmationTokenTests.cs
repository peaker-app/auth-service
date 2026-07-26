using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.UnitTests.TestData;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.EmailConfirmations;

public sealed class EmailConfirmationTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_StoresOnlyTheHashAndLeavesTheTokenActive()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        token.TokenHash.Should().Be(EmailConfirmationTokenMother.TokenHash);
        token.ConsumedAtUtc.Should().BeNull();
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        token.IsActive(Now.Add(EmailConfirmationTokenMother.Lifetime)).Should().BeFalse();
    }

    [Fact]
    public void Consume_OnActiveToken_MarksItAsConsumed()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        Result result = token.Consume(Now);

        result.IsSuccess.Should().BeTrue();
        token.ConsumedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Consume_OnAlreadyConsumedToken_ReturnsInvalidOrExpired()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);
        token.Consume(Now);

        Result result = token.Consume(Now.AddMinutes(1));

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public void Consume_OnExpiredToken_ReturnsInvalidOrExpired()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        Result result = token.Consume(Now.Add(EmailConfirmationTokenMother.Lifetime).AddSeconds(1));

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public void Invalidate_OnActiveToken_PreventsItsLaterUse()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        token.Invalidate(Now);

        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Invalidate_OnConsumedToken_KeepsTheOriginalConsumption()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);
        token.Consume(Now);

        token.Invalidate(Now.AddHours(1));

        token.ConsumedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void AllowsReissue_WithinTheCooldown_ReturnsFalse()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        token.AllowsReissue(Now.Add(EmailConfirmationToken.ResendCooldown).AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void AllowsReissue_OnceTheCooldownElapsed_ReturnsTrue()
    {
        EmailConfirmationToken token = EmailConfirmationTokenMother.Active(Now);

        token.AllowsReissue(Now.Add(EmailConfirmationToken.ResendCooldown)).Should().BeTrue();
    }
}

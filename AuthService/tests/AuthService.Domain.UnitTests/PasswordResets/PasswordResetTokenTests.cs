using AuthService.Domain.PasswordResets;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.PasswordResets;

public sealed class PasswordResetTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_CreatesAnActiveToken()
    {
        PasswordResetToken token = Issue();

        token.IsActive(Now).Should().BeTrue();
        token.ConsumedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Consume_OnAnActiveToken_MarksItAsUsed()
    {
        PasswordResetToken token = Issue();

        Result result = token.Consume(Now);

        result.IsSuccess.Should().BeTrue();
        token.ConsumedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Consume_Twice_IsRejected()
    {
        PasswordResetToken token = Issue();
        token.Consume(Now);

        Result result = token.Consume(Now);

        result.Error.Should().Be(PasswordResetErrors.InvalidOrExpired);
    }

    [Fact]
    public void Consume_AfterExpiry_IsRejected()
    {
        PasswordResetToken token = Issue();

        Result result = token.Consume(Now.AddHours(2));

        result.Error.Should().Be(PasswordResetErrors.InvalidOrExpired);
    }

    [Fact]
    public void Invalidate_OnAnActiveToken_PreventsFurtherUse()
    {
        PasswordResetToken token = Issue();

        token.Invalidate(Now);

        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Invalidate_OnAConsumedToken_KeepsTheOriginalTimestamp()
    {
        PasswordResetToken token = Issue();
        token.Consume(Now);

        token.Invalidate(Now.AddMinutes(5));

        token.ConsumedAtUtc.Should().Be(Now);
    }

    private static PasswordResetToken Issue() =>
        PasswordResetToken.Issue(new PasswordResetTokenDraft(
            Guid.CreateVersion7(), "reset-hash", Now, Now.AddHours(1)));
}

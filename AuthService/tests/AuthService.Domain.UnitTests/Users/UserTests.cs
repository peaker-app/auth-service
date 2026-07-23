using AuthService.Domain.UnitTests.TestData;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_WithValidEmailAndHash_CreatesActiveUnconfirmedUser()
    {
        Result<User> result = User.Register(TestEmail.Create(), UserMother.PasswordHash);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserStatus.Active);
        result.Value.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Register_RaisesUserRegisteredDomainEvent()
    {
        Email email = TestEmail.Create();

        Result<User> result = User.Register(email, UserMother.PasswordHash);

        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.Email.Should().Be(email.Value);
    }

    [Fact]
    public void Register_WithEmptyHash_ReturnsPasswordHashMissing()
    {
        Result<User> result = User.Register(TestEmail.Create(), "   ");

        result.Error.Should().Be(UserErrors.PasswordHashMissing);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_DoesNotLock()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts - 1, Now);

        user.LockedUntilUtc.Should().BeNull();
        user.IsLockedOut(Now).Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_ReachingThreshold_LocksForOneMinute()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts, Now);

        user.Status.Should().Be(UserStatus.Locked);
        user.LockedUntilUtc.Should().Be(Now.AddMinutes(1));
        user.IsLockedOut(Now).Should().BeTrue();
    }

    [Fact]
    public void RecordFailedLogin_SecondLockoutCycle_GrowsWaitTime()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts * 2, Now);

        user.LockedUntilUtc.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void RecordSuccessfulLogin_AfterLockout_ResetsCounterAndUnlocks()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts, Now);

        user.RecordSuccessfulLogin();

        user.FailedLoginCount.Should().Be(0);
        user.LockedUntilUtc.Should().BeNull();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void IsLockedOut_AfterLockWindowElapsed_ReturnsFalse()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts, Now);

        user.IsLockedOut(Now.AddMinutes(2)).Should().BeFalse();
    }
}

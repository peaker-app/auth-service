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
    public void Register_WithValidCredentials_CreatesActiveUnconfirmedUser()
    {
        Result<User> result = User.Register(TestEmail.Create(), TestUsername.Create(), UserMother.PasswordHash);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserStatus.Active);
        result.Value.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Register_KeepsTheUsernameAsTyped()
    {
        Result<User> result = User.Register(TestEmail.Create(), TestUsername.Create(), UserMother.PasswordHash);

        result.Value.Username.Value.Should().Be(TestUsername.Raw);
    }

    [Fact]
    public void Register_RaisesUserRegisteredDomainEventCarryingTheUsername()
    {
        Result<User> result = User.Register(TestEmail.Create(), TestUsername.Create(), UserMother.PasswordHash);

        result.Value.DomainEvents.OfType<UserRegisteredDomainEvent>().Should().ContainSingle()
            .Which.Username.Should().Be(TestUsername.Raw);
    }

    [Fact]
    public void Register_RaisesUserRegisteredDomainEventCarryingTheEmail()
    {
        Email email = TestEmail.Create();

        Result<User> result = User.Register(email, TestUsername.Create(), UserMother.PasswordHash);

        result.Value.DomainEvents.OfType<UserRegisteredDomainEvent>().Should().ContainSingle()
            .Which.Email.Should().Be(email.Value);
    }

    [Fact]
    public void Register_RaisesEmailConfirmationRequestedDomainEvent()
    {
        Result<User> result = User.Register(TestEmail.Create(), TestUsername.Create(), UserMother.PasswordHash);

        result.Value.DomainEvents.OfType<EmailConfirmationRequestedDomainEvent>().Should().ContainSingle()
            .Which.UserId.Should().Be(result.Value.Id);
    }

    [Fact]
    public void ConfirmEmail_OnUnconfirmedAccount_MarksTheEmailAsConfirmed()
    {
        User user = UserMother.Registered();

        Result result = user.ConfirmEmail();

        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmail_OnUnconfirmedAccount_RaisesUserEmailConfirmedDomainEvent()
    {
        User user = UserMother.Registered();

        user.ConfirmEmail();

        user.DomainEvents.OfType<UserEmailConfirmedDomainEvent>().Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_ReturnsEmailAlreadyConfirmed()
    {
        User user = UserMother.Confirmed();

        Result result = user.ConfirmEmail();

        result.Error.Should().Be(UserErrors.EmailAlreadyConfirmed);
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_DoesNotRaiseTheEventTwice()
    {
        User user = UserMother.Confirmed();

        user.ConfirmEmail();

        user.DomainEvents.OfType<UserEmailConfirmedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Register_WithEmptyHash_ReturnsPasswordHashMissing()
    {
        Result<User> result = User.Register(TestEmail.Create(), TestUsername.Create(), "   ");

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

    [Fact]
    public void Delete_OnActiveAccount_MarksTheAccountAsDeleted()
    {
        User user = UserMother.Registered();

        Result result = user.Delete();

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public void Delete_OnActiveAccount_RaisesUserDeletedDomainEvent()
    {
        User user = UserMother.Registered();

        user.Delete();

        user.DomainEvents.OfType<UserDeletedDomainEvent>().Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ReturnsAlreadyDeleted()
    {
        User user = UserMother.Deleted();

        Result result = user.Delete();

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_DoesNotRaiseTheEventTwice()
    {
        User user = UserMother.Deleted();

        user.Delete();

        user.DomainEvents.OfType<UserDeletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void CanSignIn_OnActiveAccount_ReturnsTrue()
    {
        User user = UserMother.Registered();

        user.CanSignIn(Now).Should().BeTrue();
    }

    [Fact]
    public void CanSignIn_WhenDeleted_ReturnsFalse()
    {
        User user = UserMother.Deleted();

        user.CanSignIn(Now).Should().BeFalse();
    }

    [Fact]
    public void CanSignIn_WhenLockedOut_ReturnsFalse()
    {
        User user = UserMother.WithFailedLogins(User.MaxFailedAttempts, Now);

        user.CanSignIn(Now).Should().BeFalse();
    }
}

using AuthService.Domain.UnitTests.TestData;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class UserTests
{

    [Fact]
    public void Register_WithValidCredentials_CreatesActiveUnconfirmedUser()
    {
        Result<User> result = User.Register(UserMother.Draft());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserStatus.Active);
        result.Value.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Register_KeepsTheUsernameAsTyped()
    {
        Result<User> result = User.Register(UserMother.Draft());

        result.Value.Username.Value.Should().Be(TestUsername.Raw);
    }

    [Fact]
    public void Register_RaisesUserRegisteredDomainEventCarryingTheUsername()
    {
        Result<User> result = User.Register(UserMother.Draft());

        result.Value.DomainEvents.OfType<UserRegisteredDomainEvent>().Should().ContainSingle()
            .Which.Username.Should().Be(TestUsername.Raw);
    }

    [Fact]
    public void Register_RaisesUserRegisteredDomainEventCarryingTheEmail()
    {
        Email email = TestEmail.Create();

        Result<User> result = User.Register(UserMother.Draft() with { Email = email });

        result.Value.DomainEvents.OfType<UserRegisteredDomainEvent>().Should().ContainSingle()
            .Which.Email.Should().Be(email.Value);
    }

    [Fact]
    public void Register_RaisesEmailConfirmationRequestedDomainEvent()
    {
        Result<User> result = User.Register(UserMother.Draft());

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
        Result<User> result = User.Register(UserMother.Draft("   "));

        result.Error.Should().Be(UserErrors.PasswordHashMissing);
    }

    [Fact]
    public void Delete_OnActiveAccount_MarksTheAccountAsDeleted()
    {
        User user = UserMother.Registered();

        Result result = user.Delete(TestEmail.Pseudonym());

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public void Delete_OnActiveAccount_ReplacesTheEmailWithThePseudonym()
    {
        User user = UserMother.Registered();

        user.Delete(TestEmail.Pseudonym());

        user.Email.Value.Should().Be(TestEmail.PseudonymValue);
    }

    [Fact]
    public void Delete_OnActiveAccount_RaisesUserDeletedDomainEvent()
    {
        User user = UserMother.Registered();

        user.Delete(TestEmail.Pseudonym());

        user.DomainEvents.OfType<UserDeletedDomainEvent>().Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ReturnsAlreadyDeleted()
    {
        User user = UserMother.Deleted();

        Result result = user.Delete(TestEmail.Pseudonym());

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_DoesNotRaiseTheEventTwice()
    {
        User user = UserMother.Deleted();

        user.Delete(TestEmail.Pseudonym());

        user.DomainEvents.OfType<UserDeletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void CanSignIn_OnActiveAccount_ReturnsTrue()
    {
        User user = UserMother.Registered();

        user.CanSignIn.Should().BeTrue();
    }

    [Fact]
    public void CanSignIn_WhenDeleted_ReturnsFalse()
    {
        User user = UserMother.Deleted();

        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public void CanSignIn_WhenLocked_ReturnsFalse()
    {
        User user = UserMother.Locked();

        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_OnActiveAccount_ReplacesTheHash()
    {
        User user = UserMother.Registered();

        Result result = user.ChangePassword("argon2id$new");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("argon2id$new");
    }

    [Fact]
    public void ChangePassword_WithEmptyHash_ReturnsPasswordHashMissing()
    {
        User user = UserMother.Registered();

        Result result = user.ChangePassword("   ");

        result.Error.Should().Be(UserErrors.PasswordHashMissing);
    }

    [Fact]
    public void ChangePassword_OnADeletedAccount_IsRejected()
    {
        User user = UserMother.Deleted();

        Result result = user.ChangePassword("argon2id$new");

        result.Error.Should().Be(UserErrors.CannotSignIn);
        user.PasswordHash.Should().Be(UserMother.PasswordHash);
    }

    [Fact]
    public void RequestPasswordReset_OnActiveAccount_RaisesTheDomainEvent()
    {
        User user = UserMother.Registered();

        Result result = user.RequestPasswordReset();

        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.OfType<PasswordResetRequestedDomainEvent>().Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void RequestPasswordReset_OnALockedAccount_IsRejected()
    {
        User user = UserMother.Locked();

        Result result = user.RequestPasswordReset();

        result.Error.Should().Be(UserErrors.CannotSignIn);
        user.DomainEvents.OfType<PasswordResetRequestedDomainEvent>().Should().BeEmpty();
    }
}

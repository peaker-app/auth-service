using AuthService.Domain.UnitTests.TestData;
using AuthService.Domain.Users;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class UserUnlockTests
{
    [Fact]
    public void Lock_OnAnActiveAccount_BlocksSignIn()
    {
        User user = UserMother.Registered();

        Result result = user.Lock();

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Locked);
        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public void Lock_OnAnAlreadyLockedAccount_Fails()
    {
        User user = UserMother.Locked();

        Result result = user.Lock();

        result.Error.Should().Be(UserErrors.AlreadyLocked);
    }

    [Fact]
    public void Lock_OnADeletedAccount_Fails()
    {
        User user = UserMother.Deleted();

        Result result = user.Lock();

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public void Unlock_OnALockedAccount_RestoresIt()
    {
        User user = UserMother.Locked();

        Result result = user.Unlock();

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.CanSignIn.Should().BeTrue();
    }

    [Fact]
    public void Unlock_OnAnActiveAccount_Fails()
    {
        User user = UserMother.Registered();

        Result result = user.Unlock();

        result.Error.Should().Be(UserErrors.NotLocked);
    }

    [Fact]
    public void Unlock_OnADeletedAccount_Fails()
    {
        User user = UserMother.Deleted();

        Result result = user.Unlock();

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        user.IsDeleted.Should().BeTrue();
    }
}

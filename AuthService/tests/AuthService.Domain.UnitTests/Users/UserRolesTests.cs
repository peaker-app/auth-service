using AuthService.Domain.UnitTests.TestData;
using AuthService.Domain.Users;
using Common.Domain.Results;
using FluentAssertions;
using Xunit;

namespace AuthService.Domain.UnitTests.Users;

public sealed class UserRolesTests
{
    [Fact]
    public void Register_LeavesTheAccountWithoutRoles()
    {
        User user = UserMother.Registered();

        user.Roles.Should().BeEmpty();
        user.IsInRole(UserRole.Admin).Should().BeFalse();
    }

    [Fact]
    public void Grant_OnAnAccountWithoutTheRole_AddsIt()
    {
        User user = UserMother.Registered();

        Result result = user.Grant(UserRole.Admin);

        result.IsSuccess.Should().BeTrue();
        user.Roles.Should().Equal(UserRole.Admin);
    }

    [Fact]
    public void Grant_OnAnAccountThatAlreadyHasTheRole_Fails()
    {
        User user = UserMother.Admin();

        Result result = user.Grant(UserRole.Admin);

        result.Error.Should().Be(UserErrors.RoleAlreadyGranted);
        user.Roles.Should().ContainSingle();
    }

    [Fact]
    public void Grant_OnADeletedAccount_Fails()
    {
        User user = UserMother.Deleted();

        Result result = user.Grant(UserRole.Admin);

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Revoke_OnAnAccountWithTheRole_RemovesIt()
    {
        User user = UserMother.Admin();

        Result result = user.Revoke(UserRole.Admin);

        result.IsSuccess.Should().BeTrue();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Revoke_OnAnAccountWithoutTheRole_Fails()
    {
        User user = UserMother.Registered();

        Result result = user.Revoke(UserRole.Admin);

        result.Error.Should().Be(UserErrors.RoleNotGranted);
    }
}

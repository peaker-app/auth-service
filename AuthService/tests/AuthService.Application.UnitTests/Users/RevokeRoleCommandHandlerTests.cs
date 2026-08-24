using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.RevokeRole;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class RevokeRoleCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RevokeRoleCommandHandler _handler;

    public RevokeRoleCommandHandlerTests() => _handler = new RevokeRoleCommandHandler(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WhenActorIsAdmin_RevokesTheRoleFromAnotherAccount()
    {
        User admin = Factories.AdminUser();
        User target = Factories.AdminUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(
            new RevokeRoleCommand(admin.Id, target.Id, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsInRole(UserRole.Admin).Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAdminRevokesTheirOwnAdminRole_IsRejected()
    {
        User admin = Factories.AdminUser();
        _userRepository.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);

        Result result = await _handler.Handle(
            new RevokeRoleCommand(admin.Id, admin.Id, UserRole.Admin), CancellationToken.None);

        result.Error.Should().Be(UserErrors.LastAdminRoleRevoked);
        admin.IsInRole(UserRole.Admin).Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorIsNotAdmin_ReturnsForbidden()
    {
        User actor = Factories.ActiveUser();
        User target = Factories.AdminUser();
        GivenUsers(actor, target);

        Result result = await _handler.Handle(
            new RevokeRoleCommand(actor.Id, target.Id, UserRole.Admin), CancellationToken.None);

        result.Error.Should().Be(UserErrors.AdminRoleRequired);
        target.IsInRole(UserRole.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTargetDoesNotHaveTheRole_ReturnsConflict()
    {
        User admin = Factories.AdminUser();
        User target = Factories.ActiveUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(
            new RevokeRoleCommand(admin.Id, target.Id, UserRole.Admin), CancellationToken.None);

        result.Error.Should().Be(UserErrors.RoleNotGranted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void GivenUsers(User actor, User target)
    {
        _userRepository.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        _userRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
    }
}

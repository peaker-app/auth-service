using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.GrantRole;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class GrantRoleCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GrantRoleCommandHandler _handler;

    public GrantRoleCommandHandlerTests() => _handler = new GrantRoleCommandHandler(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WhenActorIsAdmin_GrantsTheRole()
    {
        User admin = Factories.AdminUser();
        User target = Factories.ActiveUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(CommandFor(admin, target), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsInRole(UserRole.Admin).Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorIsNotAdmin_ReturnsForbiddenWithoutPersisting()
    {
        User actor = Factories.ActiveUser();
        User target = Factories.ActiveUser();
        GivenUsers(actor, target);

        Result result = await _handler.Handle(CommandFor(actor, target), CancellationToken.None);

        result.Error.Should().Be(UserErrors.AdminRoleRequired);
        target.IsInRole(UserRole.Admin).Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorDoesNotExist_ReturnsForbidden()
    {
        User target = Factories.ActiveUser();
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(new GrantRoleCommand(Guid.CreateVersion7(), target.Id, UserRole.Admin),
            CancellationToken.None);

        result.Error.Should().Be(UserErrors.AdminRoleRequired);
    }

    [Fact]
    public async Task Handle_WhenTargetDoesNotExist_ReturnsNotFound()
    {
        User admin = Factories.AdminUser();
        Guid missing = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
        _userRepository.GetByIdAsync(missing, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(
            new GrantRoleCommand(admin.Id, missing, UserRole.Admin), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(missing));
    }

    [Fact]
    public async Task Handle_WhenTargetAlreadyHasTheRole_ReturnsConflict()
    {
        User admin = Factories.AdminUser();
        User target = Factories.AdminUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(CommandFor(admin, target), CancellationToken.None);

        result.Error.Should().Be(UserErrors.RoleAlreadyGranted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static GrantRoleCommand CommandFor(User actor, User target) =>
        new(actor.Id, target.Id, UserRole.Admin);

    private void GivenUsers(User actor, User target)
    {
        _userRepository.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        _userRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
    }
}

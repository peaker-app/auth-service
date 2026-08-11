using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.UnlockUser;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class UnlockUserCommandHandlerTests
{

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UnlockUserCommandHandler _handler;

    public UnlockUserCommandHandlerTests() => _handler = new UnlockUserCommandHandler(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WhenActorIsAdmin_UnlocksTheAccount()
    {
        User admin = Factories.AdminUser();
        User target = Factories.LockedUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(new UnlockUserCommand(admin.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.CanSignIn.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorIsNotAdmin_ReturnsForbiddenAndLeavesTheAccountLocked()
    {
        User actor = Factories.ActiveUser();
        User target = Factories.LockedUser();
        GivenUsers(actor, target);

        Result result = await _handler.Handle(new UnlockUserCommand(actor.Id, target.Id), CancellationToken.None);

        result.Error.Should().Be(UserErrors.AdminRoleRequired);
        target.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTargetDoesNotExist_ReturnsNotFound()
    {
        User admin = Factories.AdminUser();
        Guid missing = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
        _userRepository.GetByIdAsync(missing, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(new UnlockUserCommand(admin.Id, missing), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(missing));
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotLocked_ReturnsConflict()
    {
        User admin = Factories.AdminUser();
        User target = Factories.ActiveUser();
        GivenUsers(admin, target);

        Result result = await _handler.Handle(new UnlockUserCommand(admin.Id, target.Id), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotLocked);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void GivenUsers(User actor, User target)
    {
        _userRepository.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        _userRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
    }
}

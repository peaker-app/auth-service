using AuthService.Application.Authentication;
using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.LogoutAllSessions;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class LogoutAllSessionsCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISessionRevoker _sessionRevoker = Substitute.For<ISessionRevoker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LogoutAllSessionsCommandHandler _handler;

    public LogoutAllSessionsCommandHandlerTests() =>
        _handler = new LogoutAllSessionsCommandHandler(_userRepository, _sessionRevoker, _unitOfWork);

    [Fact]
    public async Task Handle_WithAKnownUser_RevokesEverySession()
    {
        User user = Factories.ActiveUser();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result result = await _handler.Handle(new LogoutAllSessionsCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _sessionRevoker.Received(1).RevokeAllAsync(user, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownUser_SucceedsWithoutRevoking()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(
            new LogoutAllSessionsCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _sessionRevoker.DidNotReceive().RevokeAllAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

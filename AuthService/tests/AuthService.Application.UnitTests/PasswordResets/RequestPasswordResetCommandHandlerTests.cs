using AuthService.Application.PasswordResets.RequestPasswordReset;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.PasswordResets;

public sealed class RequestPasswordResetCommandHandlerTests
{
    private static readonly RequestPasswordResetCommand Command = new(Factories.DefaultEmail);

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RequestPasswordResetCommandHandler _handler;

    public RequestPasswordResetCommandHandlerTests() =>
        _handler = new RequestPasswordResetCommandHandler(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_WithAnExistingAccount_RaisesTheResetRequest()
    {
        User user = Factories.ActiveUser();
        GivenUserFound(user);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.OfType<PasswordResetRequestedDomainEvent>().Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownAccount_SucceedsWithoutTouchingAnything()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithADeletedAccount_SucceedsWithoutRaisingTheRequest()
    {
        User user = Factories.DeletedUser();
        GivenUserFound(user);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.OfType<PasswordResetRequestedDomainEvent>().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithALockedAccount_SucceedsWithoutRaisingTheRequest()
    {
        User user = Factories.LockedUser();
        GivenUserFound(user);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.OfType<PasswordResetRequestedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAMalformedEmail_ReturnsValidationError()
    {
        Result result = await _handler.Handle(
            new RequestPasswordResetCommand("not-an-email"), CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailInvalid);
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    private void GivenUserFound(User user) =>
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
}

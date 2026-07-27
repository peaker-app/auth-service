using AuthService.Domain.Users.Events;
using AuthService.Infrastructure.Messaging;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Xunit;

namespace AuthService.IntegrationTests.Messaging;

public sealed class UserRegisteredDomainEventHandlerTests
{
    private static readonly DateTime OccurredAtUtc = new(2026, 7, 27, 8, 30, 0, DateTimeKind.Utc);

    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly UserRegisteredDomainEventHandler _handler;

    public UserRegisteredDomainEventHandlerTests() => _handler = new UserRegisteredDomainEventHandler(_publishEndpoint);

    [Fact]
    public async Task Handle_PublishesTheEventUnderTheOutboxMessageId()
    {
        DomainEventContext context = new(Guid.CreateVersion7(), OccurredAtUtc);

        await _handler.Handle(NewDomainEvent(), context, CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserRegistered>(message => message!.MessageId == context.MessageId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TwiceForTheSameOutboxRow_PublishesTheSameMessageId()
    {
        DomainEventContext context = new(Guid.CreateVersion7(), OccurredAtUtc);
        UserRegisteredDomainEvent domainEvent = NewDomainEvent();

        await _handler.Handle(domainEvent, context, CancellationToken.None);
        await _handler.Handle(domainEvent, context, CancellationToken.None);

        await _publishEndpoint.Received(2).Publish(
            Arg.Is<UserRegistered>(message => message!.MessageId == context.MessageId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesTheEventUnderTheOutboxOccurrenceTime()
    {
        DomainEventContext context = new(Guid.CreateVersion7(), OccurredAtUtc);

        await _handler.Handle(NewDomainEvent(), context, CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserRegistered>(message => message!.OccurredAtUtc == OccurredAtUtc),
            Arg.Any<CancellationToken>());
    }

    private static UserRegisteredDomainEvent NewDomainEvent() =>
        new(Guid.CreateVersion7(), "hiker@peaker.io", "hiker");
}

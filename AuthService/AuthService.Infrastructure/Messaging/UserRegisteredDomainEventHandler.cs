using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserRegisteredDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(
        UserRegisteredDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserRegistered
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                UserId = domainEvent.UserId,
                Email = domainEvent.Email,
                Username = domainEvent.Username
            },
            cancellationToken);
}

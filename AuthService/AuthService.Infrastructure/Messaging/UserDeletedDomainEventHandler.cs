using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserDeletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<UserDeletedDomainEvent>
{
    public Task Handle(
        UserDeletedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserDeleted
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                UserId = domainEvent.UserId
            },
            cancellationToken);
}

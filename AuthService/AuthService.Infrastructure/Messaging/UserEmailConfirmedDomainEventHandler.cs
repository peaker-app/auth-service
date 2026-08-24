using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserEmailConfirmedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<UserEmailConfirmedDomainEvent>
{
    public Task Handle(
        UserEmailConfirmedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserEmailConfirmed
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                UserId = domainEvent.UserId
            },
            cancellationToken);
}

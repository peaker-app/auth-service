using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserDeletedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<UserDeletedDomainEvent>
{
    public Task Handle(UserDeletedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserDeleted
            {
                UserId = domainEvent.UserId,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}

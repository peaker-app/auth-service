using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserEmailConfirmedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<UserEmailConfirmedDomainEvent>
{
    public Task Handle(UserEmailConfirmedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserEmailConfirmed
            {
                UserId = domainEvent.UserId,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}

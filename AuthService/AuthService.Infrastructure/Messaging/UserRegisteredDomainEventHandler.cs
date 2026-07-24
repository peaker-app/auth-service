using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using MassTransit;

namespace AuthService.Infrastructure.Messaging;

internal sealed class UserRegisteredDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new UserRegistered
            {
                UserId = domainEvent.UserId,
                Email = domainEvent.Email,
                Username = domainEvent.Username,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}

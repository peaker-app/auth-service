using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record UserEmailConfirmedDomainEvent(Guid UserId) : IDomainEvent;

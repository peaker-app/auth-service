using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record UserDeletedDomainEvent(Guid UserId) : IDomainEvent;

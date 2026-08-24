using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string Username) : IDomainEvent;

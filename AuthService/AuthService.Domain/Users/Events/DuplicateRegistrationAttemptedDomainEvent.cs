using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record DuplicateRegistrationAttemptedDomainEvent(Guid UserId) : IDomainEvent;

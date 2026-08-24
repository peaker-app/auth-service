using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record PasswordResetRequestedDomainEvent(Guid UserId) : IDomainEvent;

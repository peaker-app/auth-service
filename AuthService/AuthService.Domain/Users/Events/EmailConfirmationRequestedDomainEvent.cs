using Common.Domain.Abstractions;

namespace AuthService.Domain.Users.Events;

public sealed record EmailConfirmationRequestedDomainEvent(Guid UserId) : IDomainEvent;

using AuthService.Application.EmailConfirmations.NotifyExistingAccount;
using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;

namespace AuthService.Infrastructure.Messaging;

internal sealed class DuplicateRegistrationAttemptedDomainEventHandler(ISender sender)
    : IDomainEventHandler<DuplicateRegistrationAttemptedDomainEvent>
{
    public async Task Handle(
        DuplicateRegistrationAttemptedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new NotifyExistingAccountCommand(domainEvent.UserId), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudo avisar al usuario {domainEvent.UserId} de que ya tiene cuenta: {result.Error.Code}.");
        }
    }
}

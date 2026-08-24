using AuthService.Application.EmailConfirmations.IssueEmailConfirmation;
using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;

namespace AuthService.Infrastructure.Messaging;

internal sealed class EmailConfirmationRequestedDomainEventHandler(ISender sender)
    : IDomainEventHandler<EmailConfirmationRequestedDomainEvent>
{
    public async Task Handle(
        EmailConfirmationRequestedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new IssueEmailConfirmationCommand(domainEvent.UserId), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudo enviar la confirmación de correo del usuario {domainEvent.UserId}: {result.Error.Code}.");
        }
    }
}

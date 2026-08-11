using AuthService.Application.PasswordResets.IssuePasswordReset;
using AuthService.Domain.Users.Events;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;

namespace AuthService.Infrastructure.Messaging;

internal sealed class PasswordResetRequestedDomainEventHandler(ISender sender)
    : IDomainEventHandler<PasswordResetRequestedDomainEvent>
{
    public async Task Handle(
        PasswordResetRequestedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new IssuePasswordResetCommand(domainEvent.UserId), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudo enviar el correo de recuperación del usuario {domainEvent.UserId}: {result.Error.Code}.");
        }
    }
}

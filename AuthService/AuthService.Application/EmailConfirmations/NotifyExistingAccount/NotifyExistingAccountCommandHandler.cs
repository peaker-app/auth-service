using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Messaging;
using Common.Domain.Results;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.EmailConfirmations.NotifyExistingAccount;

internal sealed class NotifyExistingAccountCommandHandler(
    IUserRepository userRepository,
    IExistingAccountEmailSender emailSender,
    IEmailQuota emailQuota,
    ILogger<NotifyExistingAccountCommandHandler> logger) : ICommandHandler<NotifyExistingAccountCommand>
{
    public async Task<Result> Handle(NotifyExistingAccountCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Success();
        }

        EmailQuotaVerdict verdict = await emailQuota.TryReserveAsync(user.Email.Value, cancellationToken);

        return verdict switch
        {
            EmailQuotaVerdict.Allowed => await DeliverAsync(user, cancellationToken),
            EmailQuotaVerdict.RecipientExhausted => Dropped(command.UserId),
            _ => Result.Failure(EmailConfirmationErrors.GlobalQuotaExceeded)
        };
    }

    private async Task<Result> DeliverAsync(User user, CancellationToken cancellationToken)
    {
        bool delivered = await emailSender.SendAsync(user.Email.Value, cancellationToken);

        return delivered ? Result.Success() : Result.Failure(EmailConfirmationErrors.DeliveryFailed);
    }

    private Result Dropped(Guid userId)
    {
        logger.LogWarning("Existing account notice for {UserId} dropped: recipient quota exhausted", userId);

        return Result.Success();
    }
}

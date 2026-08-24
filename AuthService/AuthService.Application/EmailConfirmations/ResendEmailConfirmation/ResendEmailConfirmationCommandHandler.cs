using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.EmailConfirmations.ResendEmailConfirmation;

internal sealed class ResendEmailConfirmationCommandHandler(
    IUserRepository userRepository,
    IEmailConfirmationTokenRepository tokenRepository,
    IEmailConfirmationIssuer emailConfirmationIssuer,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResendEmailConfirmationCommand>
{
    public async Task<Result> Handle(ResendEmailConfirmationCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);
        }

        EmailConfirmationToken? latest = await tokenRepository.GetLatestByUserAsync(user.Id, cancellationToken);

        return latest is not null && !latest.AllowsReissue(dateTimeProvider.UtcNow)
            ? Result.Failure(EmailConfirmationErrors.ResendTooSoon)
            : await emailConfirmationIssuer.IssueAsync(user, cancellationToken);
    }
}

using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.EmailConfirmations.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(
    IEmailConfirmationTokenRepository tokenRepository,
    IUserRepository userRepository,
    IEmailConfirmationTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = tokenGenerator.Hash(command.Token);
        EmailConfirmationToken? token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return Result.Failure(EmailConfirmationErrors.InvalidOrExpired);
        }

        Result consumption = token.Consume(dateTimeProvider.UtcNow);

        if (consumption.IsFailure)
        {
            return consumption;
        }

        return await ConfirmUserAsync(token.UserId, cancellationToken);
    }

    private async Task<Result> ConfirmUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(EmailConfirmationErrors.InvalidOrExpired);
        }

        Result confirmation = user.ConfirmEmail();

        if (confirmation.IsFailure)
        {
            return confirmation;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

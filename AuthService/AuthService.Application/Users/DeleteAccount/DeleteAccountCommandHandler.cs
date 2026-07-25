using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.DeleteAccount;

internal sealed class DeleteAccountCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteAccountCommand>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        Result deletion = user.Delete();

        if (deletion.IsFailure)
        {
            return deletion;
        }

        await RevokeAllSessionsAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RefreshToken> activeTokens =
            await refreshTokenRepository.GetActiveByUserAsync(userId, cancellationToken);

        DateTime utcNow = dateTimeProvider.UtcNow;

        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke(utcNow);
        }
    }
}

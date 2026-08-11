using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.UnlockUser;

internal sealed class UnlockUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UnlockUserCommand>
{
    public async Task<Result> Handle(UnlockUserCommand command, CancellationToken cancellationToken)
    {
        Result authorization = await AdminAuthorization.EnsureAdminAsync(
            userRepository, command.ActorId, cancellationToken);

        if (authorization.IsFailure)
        {
            return authorization;
        }

        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        return user is null
            ? Result.Failure(UserErrors.NotFound(command.UserId))
            : await UnlockAsync(user, cancellationToken);
    }

    private async Task<Result> UnlockAsync(User user, CancellationToken cancellationToken)
    {
        Result unlocked = user.Unlock();

        if (unlocked.IsFailure)
        {
            return unlocked;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using AuthService.Application.Authentication;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.LockUser;

internal sealed class LockUserCommandHandler(
    IUserRepository userRepository,
    ISessionRevoker sessionRevoker,
    IUnitOfWork unitOfWork) : ICommandHandler<LockUserCommand>
{
    public async Task<Result> Handle(LockUserCommand command, CancellationToken cancellationToken)
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
            : await LockAsync(user, cancellationToken);
    }

    private async Task<Result> LockAsync(User user, CancellationToken cancellationToken)
    {
        Result locked = user.Lock();

        if (locked.IsFailure)
        {
            return locked;
        }

        await sessionRevoker.RevokeAllAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using AuthService.Application.Authentication;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.LogoutAllSessions;

internal sealed class LogoutAllSessionsCommandHandler(
    IUserRepository userRepository,
    ISessionRevoker sessionRevoker,
    IUnitOfWork unitOfWork) : ICommandHandler<LogoutAllSessionsCommand>
{
    public async Task<Result> Handle(LogoutAllSessionsCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Success();
        }

        await sessionRevoker.RevokeAllAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

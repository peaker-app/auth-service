using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.RevokeRole;

internal sealed class RevokeRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeRoleCommand>
{
    public async Task<Result> Handle(RevokeRoleCommand command, CancellationToken cancellationToken)
    {
        Result authorization = await AdminAuthorization.EnsureAdminAsync(
            userRepository, command.ActorId, cancellationToken);

        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (RevokesOwnAdminRole(command))
        {
            return Result.Failure(UserErrors.LastAdminRoleRevoked);
        }

        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        return user is null
            ? Result.Failure(UserErrors.NotFound(command.UserId))
            : await RevokeAsync(user, command.Role, cancellationToken);
    }

    private static bool RevokesOwnAdminRole(RevokeRoleCommand command) =>
        command.ActorId == command.UserId && command.Role is UserRole.Admin;

    private async Task<Result> RevokeAsync(User user, UserRole role, CancellationToken cancellationToken)
    {
        Result revoked = user.Revoke(role);

        if (revoked.IsFailure)
        {
            return revoked;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

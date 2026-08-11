using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.GrantRole;

internal sealed class GrantRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<GrantRoleCommand>
{
    public async Task<Result> Handle(GrantRoleCommand command, CancellationToken cancellationToken)
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
            : await GrantAsync(user, command.Role, cancellationToken);
    }

    private async Task<Result> GrantAsync(User user, UserRole role, CancellationToken cancellationToken)
    {
        Result granted = user.Grant(role);

        if (granted.IsFailure)
        {
            return granted;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

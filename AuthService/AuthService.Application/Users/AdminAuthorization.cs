using AuthService.Domain.Users;
using Common.Domain.Results;

namespace AuthService.Application.Users;

internal static class AdminAuthorization
{
    public static async Task<Result> EnsureAdminAsync(
        IUserRepository userRepository,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        User? actor = await userRepository.GetByIdAsync(actorId, cancellationToken);

        return actor is not null && actor.IsInRole(UserRole.Admin)
            ? Result.Success()
            : Result.Failure(UserErrors.AdminRoleRequired);
    }
}

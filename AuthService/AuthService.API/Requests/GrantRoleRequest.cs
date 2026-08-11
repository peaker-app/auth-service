using AuthService.Application.Users.GrantRole;
using AuthService.Domain.Users;

namespace AuthService.API.Requests;

public sealed record GrantRoleRequest(UserRole Role)
{
    public GrantRoleCommand ToCommand(Guid actorId, Guid userId) => new(actorId, userId, Role);
}

using AuthService.Domain.Users;
using Common.Application.Messaging;

namespace AuthService.Application.Users.GrantRole;

public sealed record GrantRoleCommand(Guid ActorId, Guid UserId, UserRole Role) : ICommand;

using AuthService.Domain.Users;
using Common.Application.Messaging;

namespace AuthService.Application.Users.RevokeRole;

public sealed record RevokeRoleCommand(Guid ActorId, Guid UserId, UserRole Role) : ICommand;

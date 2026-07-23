using AuthService.Application.Users.LogoutUser;

namespace AuthService.API.Requests;

public sealed record LogoutRequest(string RefreshToken)
{
    public LogoutUserCommand ToCommand(Guid userId) => new(RefreshToken, userId);
}

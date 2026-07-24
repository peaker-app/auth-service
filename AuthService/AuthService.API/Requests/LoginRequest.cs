using AuthService.Application.Users.LoginUser;

namespace AuthService.API.Requests;

public sealed record LoginRequest(string Identifier, string Password)
{
    public LoginUserCommand ToCommand(string? ipAddress) => new(Identifier, Password, ipAddress);
}

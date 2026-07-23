using AuthService.Application.Users.LoginUser;

namespace AuthService.API.Requests;

public sealed record LoginRequest(string Email, string Password)
{
    public LoginUserCommand ToCommand(string? ipAddress) => new(Email, Password, ipAddress);
}

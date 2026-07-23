using AuthService.Application.Users.RegisterUser;

namespace AuthService.API.Requests;

public sealed record RegisterRequest(string Email, string Password)
{
    public RegisterUserCommand ToCommand() => new(Email, Password);
}

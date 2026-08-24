using AuthService.Application.Users.DeleteAccount;

namespace AuthService.API.Requests;

public sealed record DeleteAccountRequest(string Password)
{
    public DeleteAccountCommand ToCommand(Guid userId) => new(userId, Password);
}

using AuthService.Domain.Users;

namespace AuthService.Application.Abstractions;

public interface IAccessTokenGenerator
{
    GeneratedAccessToken Generate(User user);
}

namespace AuthService.Application.Abstractions;

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate(DateTime utcNow);

    string Hash(string rawToken);
}

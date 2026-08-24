namespace AuthService.Application.Abstractions;

public interface IEmailConfirmationTokenGenerator
{
    GeneratedEmailConfirmationToken Generate(DateTime utcNow);

    string Hash(string rawToken);
}

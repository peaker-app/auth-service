namespace AuthService.Application.Abstractions;

public interface ITermsPolicy
{
    string CurrentVersion { get; }
}

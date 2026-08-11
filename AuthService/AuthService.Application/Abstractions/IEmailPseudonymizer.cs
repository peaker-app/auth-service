using AuthService.Domain.Users;

namespace AuthService.Application.Abstractions;

public interface IEmailPseudonymizer
{
    Email Pseudonymize(Email email);
}

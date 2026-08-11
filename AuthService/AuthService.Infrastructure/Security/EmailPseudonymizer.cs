using AuthService.Application.Abstractions;
using AuthService.Domain.Users;
using AuthService.Infrastructure.Persistence;

namespace AuthService.Infrastructure.Security;

internal sealed class EmailPseudonymizer : IEmailPseudonymizer
{
    private const string LocalPartPrefix = "deleted+";
    private const string ReservedDomain = "@peaker.invalid";

    public Email Pseudonymize(Email email) =>
        Email.Create($"{LocalPartPrefix}{SubjectHash.Of(email.Value)}{ReservedDomain}").Value;
}

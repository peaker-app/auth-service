using Common.Domain.Exceptions;

namespace AuthService.Domain.Users;

public sealed class DuplicateCredentialException(CredentialField field, Exception innerException)
    : DomainException($"A concurrent registration already took the {field}.", innerException)
{
    public CredentialField Field { get; } = field;
}

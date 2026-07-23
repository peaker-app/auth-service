using System.Text.RegularExpressions;
using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.Users;

public sealed partial class Email : ValueObject
{
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.EmailEmpty;
        }

#pragma warning disable CA1308 // Motivo: DESIGN §4.1 exige el correo normalizado a minúsculas.
        string normalized = value.Trim().ToLowerInvariant();
#pragma warning restore CA1308

        if (normalized.Length > MaxLength)
        {
            return UserErrors.EmailTooLong;
        }

        return EmailRegex().IsMatch(normalized)
            ? new Email(normalized)
            : UserErrors.EmailInvalid;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}

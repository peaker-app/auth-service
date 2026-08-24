using System.Text.RegularExpressions;
using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.Users;

public sealed partial class Username : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    private Username(string value)
    {
        Value = value;

// The username is compared in lowercase so that uniqueness ignores casing
#pragma warning disable CA1308
        NormalizedValue = value.ToLowerInvariant();
#pragma warning restore CA1308
    }

    public string Value { get; }

    public string NormalizedValue { get; }

    public static Result<Username> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.UsernameEmpty;
        }

        string trimmed = value.Trim();

        if (trimmed.Length is < MinLength or > MaxLength)
        {
            return UserErrors.UsernameInvalid;
        }

        return UsernameRegex().IsMatch(trimmed)
            ? new Username(trimmed)
            : UserErrors.UsernameInvalid;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NormalizedValue;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+([._-][a-zA-Z0-9]+)*$")]
    private static partial Regex UsernameRegex();
}

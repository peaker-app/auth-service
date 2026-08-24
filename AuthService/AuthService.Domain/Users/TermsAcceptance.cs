using Common.Domain.Abstractions;

namespace AuthService.Domain.Users;

public sealed class TermsAcceptance : ValueObject
{
    public const int MaxVersionLength = 20;

    private TermsAcceptance(string version, DateTime acceptedAtUtc)
    {
        Version = version;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public string Version { get; }

    public DateTime AcceptedAtUtc { get; }

    public static TermsAcceptance Of(string version, DateTime acceptedAtUtc) =>
        new(version.Trim(), acceptedAtUtc);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Version;
        yield return AcceptedAtUtc;
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AuthService.Infrastructure.ExternalServices;

public enum SmtpSecurity
{
    None,
    StartTls,
    SslOnConnect
}

public sealed class SmtpOptions
{
    public const string SectionName = "EmailConfirmation:Smtp";

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public SmtpSecurity Security { get; init; } = SmtpSecurity.None;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    [MemberNotNullWhen(true, nameof(Username), nameof(Password))]
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public bool IsEncrypted => Security != SmtpSecurity.None;
}

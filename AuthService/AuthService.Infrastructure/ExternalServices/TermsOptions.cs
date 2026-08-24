using System.ComponentModel.DataAnnotations;
using AuthService.Application.Abstractions;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class TermsOptions : ITermsPolicy
{
    public const string SectionName = "Terms";

    [Required]
    [StringLength(20)]
    public string Version { get; init; } = "2026-08-11";

    public string CurrentVersion => Version;
}

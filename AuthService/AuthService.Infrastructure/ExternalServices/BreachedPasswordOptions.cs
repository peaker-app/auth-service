using System.ComponentModel.DataAnnotations;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class BreachedPasswordOptions
{
    public const string SectionName = "BreachedPassword";

    [Required]
    public Uri BaseAddress { get; init; } = new("https://api.pwnedpasswords.com/");

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

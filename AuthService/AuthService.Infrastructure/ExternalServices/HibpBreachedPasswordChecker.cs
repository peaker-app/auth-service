using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class HibpBreachedPasswordChecker(
    HttpClient httpClient,
    ILogger<HibpBreachedPasswordChecker> logger) : IBreachedPasswordChecker
{
    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031 // Fail-open: si HIBP no responde no se bloquea el registro (se registra el aviso).
        try
        {
            return await QueryAsync(password, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Breached password check unavailable; allowing registration (fail-open)");
            return false;
        }
#pragma warning restore CA1031
    }

    private async Task<bool> QueryAsync(string password, CancellationToken cancellationToken)
    {
        (string prefix, string suffix) = HashToPrefixSuffix(password);

        using HttpResponseMessage response =
            await httpClient.GetAsync(new Uri($"range/{prefix}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return ContainsSuffix(body, suffix);
    }

    private static (string Prefix, string Suffix) HashToPrefixSuffix(string password)
    {
#pragma warning disable CA5350 // Motivo: la API k-anonymity de HIBP exige SHA-1; no protege datos propios.
        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
#pragma warning restore CA5350
        string hex = Convert.ToHexString(hash);

        return (hex[..5], hex[5..]);
    }

    private static bool ContainsSuffix(string body, string suffix) => body
        .Split('\n')
        .Any(line => line.StartsWith(suffix, StringComparison.OrdinalIgnoreCase));
}

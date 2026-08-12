using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class SigningKeyProvider : IDisposable
{
    private const int EphemeralKeySizeBits = 2048;

    private readonly SigningKey _current;
    private readonly SigningKey? _previous;

    public SigningKeyProvider(IOptions<AuthTokenOptions> options, ILogger<SigningKeyProvider> logger)
    {
        _current = new SigningKey(CreateKey(options.Value.PrivateKeyPem, logger));
        _previous = CreatePreviousKey(options.Value.PreviousPrivateKeyPem, _current.KeyId);
    }

    public string KeyId => _current.KeyId;

    public SigningCredentials CreateSigningCredentials() => _current.CreateSigningCredentials();

    public IReadOnlyCollection<RsaSecurityKey> CreatePublicSecurityKeys() =>
        [.. PublishedKeys.Select(key => key.CreatePublicSecurityKey())];

    public IReadOnlyCollection<JsonWebKey> CreatePublicJsonWebKeys() =>
        [.. PublishedKeys.Select(key => key.CreatePublicJsonWebKey())];

    public void Dispose()
    {
        _current.Dispose();
        _previous?.Dispose();
    }

    private IEnumerable<SigningKey> PublishedKeys => _previous is null ? [_current] : [_current, _previous];

    private static RSA CreateKey(string? privateKeyPem, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            logger.LogWarning(
                "No signing key configured: using an ephemeral one. Tokens issued now stop validating on restart");

            return RSA.Create(EphemeralKeySizeBits);
        }

        return ImportKey(privateKeyPem);
    }

    private static SigningKey? CreatePreviousKey(string? privateKeyPem, string currentKeyId)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            return null;
        }

        SigningKey key = new(ImportKey(privateKeyPem));
        if (!string.Equals(key.KeyId, currentKeyId, StringComparison.Ordinal))
        {
            return key;
        }

        key.Dispose();

        return null;
    }

    private static RSA ImportKey(string privateKeyPem)
    {
        RSA rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(privateKeyPem);
        }
        catch
        {
            rsa.Dispose();
            throw;
        }

        return rsa;
    }
}

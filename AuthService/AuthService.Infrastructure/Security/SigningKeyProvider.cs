using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class SigningKeyProvider : IDisposable
{
    private const int EphemeralKeySizeBits = 2048;

    private readonly RSA _rsa;

    public SigningKeyProvider(IOptions<AuthTokenOptions> options, ILogger<SigningKeyProvider> logger)
    {
        _rsa = CreateKey(options.Value.PrivateKeyPem, logger);
        KeyId = CreateKeyId(_rsa);
    }

    public string KeyId { get; }

    public SigningCredentials CreateSigningCredentials() =>
        new(new RsaSecurityKey(_rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256);

    public RsaSecurityKey CreatePublicSecurityKey() =>
        new(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = KeyId };

    public JsonWebKey CreatePublicJsonWebKey()
    {
        RSAParameters publicParameters = _rsa.ExportParameters(includePrivateParameters: false);
        var publicKey = new RsaSecurityKey(publicParameters) { KeyId = KeyId };

        JsonWebKey jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        jsonWebKey.Use = "sig";
        jsonWebKey.Alg = SecurityAlgorithms.RsaSha256;

        return jsonWebKey;
    }

    public void Dispose() => _rsa.Dispose();

    private static RSA CreateKey(string? privateKeyPem, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            logger.LogWarning(
                "No signing key configured: using an ephemeral one. Tokens issued now stop validating on restart");

            return RSA.Create(EphemeralKeySizeBits);
        }

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

    private static string CreateKeyId(RSA rsa)
    {
        byte[] hash = SHA256.HashData(rsa.ExportRSAPublicKey());

        return Base64UrlEncoder.Encode(hash)[..16];
    }
}

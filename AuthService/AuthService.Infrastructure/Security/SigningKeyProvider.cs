using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class SigningKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public SigningKeyProvider(IOptions<AuthTokenOptions> options)
    {
        _rsa = RSA.Create(2048);

        string? privateKeyPem = options.Value.PrivateKeyPem;
        if (!string.IsNullOrWhiteSpace(privateKeyPem))
        {
            _rsa.ImportFromPem(privateKeyPem);
        }

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

    private static string CreateKeyId(RSA rsa)
    {
        byte[] hash = SHA256.HashData(rsa.ExportRSAPublicKey());
        return Base64UrlEncoder.Encode(hash)[..16];
    }
}

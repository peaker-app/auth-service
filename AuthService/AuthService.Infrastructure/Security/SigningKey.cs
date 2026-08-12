using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class SigningKey : IDisposable
{
    private readonly RSA _rsa;

    internal SigningKey(RSA rsa)
    {
        _rsa = rsa;
        KeyId = CreateKeyId(rsa);
    }

    public string KeyId { get; }

    public SigningCredentials CreateSigningCredentials() =>
        new(new RsaSecurityKey(_rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256);

    public RsaSecurityKey CreatePublicSecurityKey() =>
        new(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = KeyId };

    public JsonWebKey CreatePublicJsonWebKey()
    {
        JsonWebKey jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(CreatePublicSecurityKey());
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

using AuthService.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class TokenMetadataProvider(
    SigningKeyProvider signingKeyProvider,
    IOptions<AuthTokenOptions> options) : ITokenMetadataProvider
{
    public string Issuer => options.Value.Issuer;

    public IReadOnlyCollection<JsonWebKeyResponse> GetSigningKeys() =>
    [
        .. signingKeyProvider.CreatePublicJsonWebKeys()
            .Select(key => new JsonWebKeyResponse(key.Kty, key.Use, key.Kid, key.Alg, key.N, key.E))
    ];
}

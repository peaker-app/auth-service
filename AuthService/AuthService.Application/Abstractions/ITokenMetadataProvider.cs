namespace AuthService.Application.Abstractions;

public sealed record JsonWebKeyResponse(string Kty, string Use, string Kid, string Alg, string N, string E);

public interface ITokenMetadataProvider
{
    string Issuer { get; }

    IReadOnlyCollection<JsonWebKeyResponse> GetSigningKeys();
}

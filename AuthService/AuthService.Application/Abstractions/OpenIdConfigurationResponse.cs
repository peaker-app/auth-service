using System.Text.Json.Serialization;

namespace AuthService.Application.Abstractions;

public sealed record OpenIdConfigurationResponse(
    [property: JsonPropertyName("issuer")] string Issuer,
    [property: JsonPropertyName("jwks_uri")] Uri JwksUri,
    [property: JsonPropertyName("id_token_signing_alg_values_supported")]
    IReadOnlyCollection<string> IdTokenSigningAlgValuesSupported,
    [property: JsonPropertyName("response_types_supported")]
    IReadOnlyCollection<string> ResponseTypesSupported,
    [property: JsonPropertyName("subject_types_supported")]
    IReadOnlyCollection<string> SubjectTypesSupported);

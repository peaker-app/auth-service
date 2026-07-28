using AuthService.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route(".well-known")]
public sealed class JwksController(ITokenMetadataProvider tokenMetadataProvider) : ControllerBase
{
    private const string SigningAlgorithm = "RS256";

    [HttpGet("jwks.json")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetJsonWebKeySet() => Ok(new { keys = tokenMetadataProvider.GetSigningKeys() });

    [HttpGet("openid-configuration")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetOpenIdConfiguration()
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}";

        var configuration = new
        {
            issuer = tokenMetadataProvider.Issuer,
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",
            id_token_signing_alg_values_supported = new[] { SigningAlgorithm },
            response_types_supported = new[] { "token" },
            subject_types_supported = new[] { "public" }
        };

        return Ok(configuration);
    }
}

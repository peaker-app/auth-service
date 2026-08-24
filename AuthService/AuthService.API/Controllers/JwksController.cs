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
    [ProducesResponseType(typeof(JsonWebKeySetResponse), StatusCodes.Status200OK)]
    public IActionResult GetJsonWebKeySet() => Ok(new JsonWebKeySetResponse(tokenMetadataProvider.GetSigningKeys()));

    [HttpGet("openid-configuration")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OpenIdConfigurationResponse), StatusCodes.Status200OK)]
    public IActionResult GetOpenIdConfiguration()
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}";

        OpenIdConfigurationResponse configuration = new(
            tokenMetadataProvider.Issuer,
            new Uri($"{baseUrl}/.well-known/jwks.json"),
            [SigningAlgorithm],
            ["token"],
            ["public"]);

        return Ok(configuration);
    }
}

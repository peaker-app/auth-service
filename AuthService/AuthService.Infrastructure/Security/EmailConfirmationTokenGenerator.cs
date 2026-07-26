using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class EmailConfirmationTokenGenerator(IOptions<EmailConfirmationOptions> options)
    : IEmailConfirmationTokenGenerator
{
    private const int TokenSizeInBytes = 32;

    public GeneratedEmailConfirmationToken Generate(DateTime utcNow)
    {
        string rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

        return new GeneratedEmailConfirmationToken(
            rawToken, Hash(rawToken), utcNow.Add(options.Value.TokenLifetime));
    }

    public string Hash(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(hash);
    }
}

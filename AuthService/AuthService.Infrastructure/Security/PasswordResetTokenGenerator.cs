using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class PasswordResetTokenGenerator(IOptions<EmailConfirmationOptions> options)
    : IPasswordResetTokenGenerator
{
    private const int TokenSizeInBytes = 32;

    public GeneratedPasswordResetToken Generate(DateTime utcNow)
    {
        string rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

        return new GeneratedPasswordResetToken(
            rawToken, Hash(rawToken), utcNow.Add(options.Value.PasswordResetTokenLifetime));
    }

    public string Hash(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(hash);
    }
}

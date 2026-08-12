using System.Security.Cryptography;
using AuthService.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AuthService.IntegrationTests.Security;

public sealed class SigningKeyProviderTests
{
    private const string Issuer = "peaker-auth";
    private const string Audience = "peaker-api";

    [Fact]
    public void KeyId_WithTheSameConfiguredKey_IsStableAcrossInstances()
    {
        string privateKeyPem = CreatePrivateKeyPem();

        using SigningKeyProvider first = CreateProvider(privateKeyPem);
        using SigningKeyProvider second = CreateProvider(privateKeyPem);

        second.KeyId.Should().Be(first.KeyId);
    }

    [Fact]
    public void KeyId_WithoutAConfiguredKey_ChangesOnEveryInstance()
    {
        using SigningKeyProvider first = CreateProvider(privateKeyPem: null);
        using SigningKeyProvider second = CreateProvider(privateKeyPem: null);

        second.KeyId.Should().NotBe(first.KeyId);
    }

    [Fact]
    public void CreatePublicJsonWebKeys_PublishesTheKeyUnderItsKeyId()
    {
        using SigningKeyProvider provider = CreateProvider(CreatePrivateKeyPem());

        provider.CreatePublicJsonWebKeys().Should().ContainSingle().Which.Kid.Should().Be(provider.KeyId);
    }

    [Fact]
    public void CreatePublicJsonWebKeys_WithAPreviousKey_PublishesBothUnderDistinctKeyIds()
    {
        using SigningKeyProvider provider = CreateProvider(CreatePrivateKeyPem(), CreatePrivateKeyPem());

        provider.CreatePublicJsonWebKeys().Select(key => key.Kid).Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void CreatePublicJsonWebKeys_WithAPreviousKeyEqualToTheCurrentOne_PublishesItOnce()
    {
        string privateKeyPem = CreatePrivateKeyPem();

        using SigningKeyProvider provider = CreateProvider(privateKeyPem, privateKeyPem);

        provider.CreatePublicJsonWebKeys().Should().ContainSingle();
    }

    [Fact]
    public void CreateSigningCredentials_WithAPreviousKey_KeepsSigningWithTheCurrentOne()
    {
        using SigningKeyProvider provider = CreateProvider(CreatePrivateKeyPem(), CreatePrivateKeyPem());

        SecurityKey signingKey = provider.CreateSigningCredentials().Key;

        signingKey.KeyId.Should().Be(provider.KeyId);
    }

    [Fact]
    public async Task CreatePublicSecurityKeys_WithAPreviousKey_StillValidatesTokensSignedWithIt()
    {
        string previousPrivateKeyPem = CreatePrivateKeyPem();
        using SigningKeyProvider provider = CreateProvider(CreatePrivateKeyPem(), previousPrivateKeyPem);

        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(
            CreateTokenSignedWith(previousPrivateKeyPem),
            new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKeys = provider.CreatePublicSecurityKeys()
            });

        result.IsValid.Should().BeTrue();
    }

    private static string CreateTokenSignedWith(string privateKeyPem)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        });
    }

    private static SigningKeyProvider CreateProvider(string? privateKeyPem, string? previousPrivateKeyPem = null) =>
        new(
            Options.Create(new AuthTokenOptions
            {
                PrivateKeyPem = privateKeyPem,
                PreviousPrivateKeyPem = previousPrivateKeyPem
            }),
            NullLogger<SigningKeyProvider>.Instance);

    private static string CreatePrivateKeyPem()
    {
        using RSA rsa = RSA.Create(2048);

        return rsa.ExportRSAPrivateKeyPem();
    }
}

using System.Security.Cryptography;
using AuthService.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthService.IntegrationTests.Security;

public sealed class SigningKeyProviderTests
{
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
    public void CreatePublicJsonWebKey_PublishesTheKeyUnderItsKeyId()
    {
        using SigningKeyProvider provider = CreateProvider(CreatePrivateKeyPem());

        provider.CreatePublicJsonWebKey().Kid.Should().Be(provider.KeyId);
    }

    private static SigningKeyProvider CreateProvider(string? privateKeyPem) =>
        new(Options.Create(new AuthTokenOptions { PrivateKeyPem = privateKeyPem }),
            NullLogger<SigningKeyProvider>.Instance);

    private static string CreatePrivateKeyPem()
    {
        using RSA rsa = RSA.Create(2048);

        return rsa.ExportRSAPrivateKeyPem();
    }
}

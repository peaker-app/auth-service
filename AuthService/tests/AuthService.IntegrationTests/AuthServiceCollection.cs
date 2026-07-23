using Xunit;

namespace AuthService.IntegrationTests;

[CollectionDefinition(nameof(AuthServiceCollection))]
public sealed class AuthServiceCollection : ICollectionFixture<AuthServiceApiFactory>;

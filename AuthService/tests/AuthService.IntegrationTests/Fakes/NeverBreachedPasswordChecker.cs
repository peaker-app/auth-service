using AuthService.Application.Abstractions;

namespace AuthService.IntegrationTests.Fakes;

internal sealed class NeverBreachedPasswordChecker : IBreachedPasswordChecker
{
    public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken) => Task.FromResult(false);
}

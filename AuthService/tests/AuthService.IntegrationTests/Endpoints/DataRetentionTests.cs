using AuthService.Application.Maintenance.PurgeExpiredData;
using Common.Domain.Results;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class DataRetentionTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Purge_WithNothingOlderThanTheWindow_RemovesNothing()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.LoginWithTokensAsync(user.Email);

        DataPurgeResponse purge = await PurgeAsync(DateTime.UtcNow);

        purge.LoginAttemptsRemoved.Should().Be(0);
        purge.RefreshTokensRemoved.Should().Be(0);
    }

    [Fact]
    public async Task Purge_RemovesLoginAttemptsPastTheirRetention()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Username, 2);

        DataPurgeResponse purge = await PurgeAsync(DateTime.UtcNow.AddDays(400));

        purge.LoginAttemptsRemoved.Should().BeGreaterThan(0);
        (await factory.CountLoginAttemptsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Purge_RemovesTheEmailDispatchLogPastItsRetention()
    {
        await factory.SeedEmailDispatchesAsync(ApiTestHelpers.UniqueEmail(), 3);

        DataPurgeResponse purge = await PurgeAsync(DateTime.UtcNow.AddDays(400));

        purge.EmailDispatchesRemoved.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Purge_RemovesSpentConfirmationTokens()
    {
        await _client.RegisterUserAsync();

        DataPurgeResponse purge = await PurgeAsync(DateTime.UtcNow.AddDays(400));

        purge.ConfirmationTokensRemoved.Should().BeGreaterThan(0);
    }

    private async Task<DataPurgeResponse> PurgeAsync(DateTime utcNow)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<DataPurgeResponse> result = await sender.Send(new PurgeExpiredDataCommand(utcNow));
        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }
}

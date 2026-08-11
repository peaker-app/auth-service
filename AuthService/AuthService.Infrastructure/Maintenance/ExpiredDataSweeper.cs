using AuthService.Application.Maintenance.PurgeExpiredData;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Maintenance;

public sealed class ExpiredDataSweeper(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<DataRetentionOptions> options,
    ILogger<ExpiredDataSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(options.Value.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
#pragma warning disable CA1031
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Expired data sweep failed");
            }
#pragma warning restore CA1031
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<DataPurgeResponse> result = await sender.Send(
            new PurgeExpiredDataCommand(dateTimeProvider.UtcNow), cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Expired data sweep reported {ErrorCode}", result.Error.Code);
            return;
        }

        logger.LogInformation(
            "Expired data sweep removed {LoginAttempts} login attempts, {EmailDispatches} email dispatches, "
            + "{RefreshTokens} closed sessions and {SpentTokens} spent tokens",
            result.Value.LoginAttemptsRemoved,
            result.Value.EmailDispatchesRemoved,
            result.Value.RefreshTokensRemoved,
            result.Value.ConfirmationTokensRemoved + result.Value.PasswordResetTokensRemoved);
    }
}

using AuthService.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Maintenance.PurgeExpiredData;

internal sealed class PurgeExpiredDataCommandHandler(IExpiredDataPurger purger)
    : ICommandHandler<PurgeExpiredDataCommand, DataPurgeResponse>
{
    public async Task<Result<DataPurgeResponse>> Handle(
        PurgeExpiredDataCommand command,
        CancellationToken cancellationToken) =>
        await purger.PurgeAsync(command.UtcNow, cancellationToken);
}

using AuthService.Application.Maintenance.PurgeExpiredData;

namespace AuthService.Application.Abstractions;

public interface IExpiredDataPurger
{
    Task<DataPurgeResponse> PurgeAsync(DateTime utcNow, CancellationToken cancellationToken);
}

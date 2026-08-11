using Common.Application.Messaging;

namespace AuthService.Application.Maintenance.PurgeExpiredData;

public sealed record PurgeExpiredDataCommand(DateTime UtcNow) : ICommand<DataPurgeResponse>;

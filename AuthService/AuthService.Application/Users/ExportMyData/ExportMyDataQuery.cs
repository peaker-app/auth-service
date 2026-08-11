using Common.Application.Messaging;

namespace AuthService.Application.Users.ExportMyData;

public sealed record ExportMyDataQuery(Guid UserId) : IQuery<AccountExportResponse>;

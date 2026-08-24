using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.ExportMyData;

internal sealed class ExportMyDataQueryHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<ExportMyDataQuery, AccountExportResponse>
{
    public async Task<Result<AccountExportResponse>> Handle(
        ExportMyDataQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<AccountExportResponse>(UserErrors.NotFound(query.UserId));
        }

        IReadOnlyCollection<RefreshToken> sessions =
            await refreshTokenRepository.GetActiveByUserAsync(user.Id, cancellationToken);

        return ToResponse(user, sessions, dateTimeProvider.UtcNow);
    }

    private static AccountExportResponse ToResponse(
        User user,
        IReadOnlyCollection<RefreshToken> sessions,
        DateTime utcNow) => new(
        user.Id,
        user.Email.Value,
        user.Username.Value,
        user.Status.ToString(),
        user.EmailConfirmed,
        [.. user.Roles.Select(role => role.ToString())],
        user.CreatedAtUtc,
        user.UpdatedAtUtc,
        [.. sessions
            .Where(session => session.IsActive(utcNow))
            .Select(session => new AccountSessionResponse(
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.CreatedByIp))]);
}

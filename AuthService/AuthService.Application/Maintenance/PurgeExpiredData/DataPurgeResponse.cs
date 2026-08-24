namespace AuthService.Application.Maintenance.PurgeExpiredData;

public sealed record DataPurgeResponse(
    int LoginAttemptsRemoved,
    int EmailDispatchesRemoved,
    int RefreshTokensRemoved,
    int ConfirmationTokensRemoved,
    int PasswordResetTokensRemoved);

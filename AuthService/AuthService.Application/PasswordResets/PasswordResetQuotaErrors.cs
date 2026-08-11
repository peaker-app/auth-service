using AuthService.Application.Abstractions;
using AuthService.Domain.PasswordResets;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets;

internal static class PasswordResetQuotaErrors
{
    public static Error For(EmailQuotaVerdict verdict) => verdict switch
    {
        EmailQuotaVerdict.RecipientExhausted => PasswordResetErrors.RecipientQuotaExceeded,
        _ => PasswordResetErrors.GlobalQuotaExceeded
    };
}

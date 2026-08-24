using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
using Common.Domain.Results;

namespace AuthService.Application.EmailConfirmations;

internal static class EmailQuotaErrors
{
    public static Error For(EmailQuotaVerdict verdict) => verdict switch
    {
        EmailQuotaVerdict.RecipientExhausted => EmailConfirmationErrors.RecipientQuotaExceeded,
        _ => EmailConfirmationErrors.GlobalQuotaExceeded
    };
}

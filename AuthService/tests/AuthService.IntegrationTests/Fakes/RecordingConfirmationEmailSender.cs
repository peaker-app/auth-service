using System.Collections.Concurrent;
using AuthService.Application.Abstractions;

namespace AuthService.IntegrationTests.Fakes;

internal sealed class RecordingConfirmationEmailSender : IConfirmationEmailSender
{
    private readonly ConcurrentDictionary<string, string> _tokensByRecipient = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
        _tokensByRecipient[recipientEmail] = rawToken;

        return Task.FromResult(true);
    }

    public string? TokenFor(string recipientEmail) =>
        _tokensByRecipient.TryGetValue(recipientEmail, out string? token) ? token : null;

    public async Task<string?> WaitForTokenAsync(string recipientEmail)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string? token = TokenFor(recipientEmail);

            if (token is not null)
            {
                return token;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return null;
    }
}

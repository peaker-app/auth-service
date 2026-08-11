using System.Collections.Concurrent;
using AuthService.Application.Abstractions;

namespace AuthService.IntegrationTests.Fakes;

internal sealed class RecordingExistingAccountEmailSender : IExistingAccountEmailSender
{
    private readonly ConcurrentDictionary<string, byte> _recipients = new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => _recipients.Clear();

    public Task<bool> SendAsync(string recipientEmail, CancellationToken cancellationToken)
    {
        _recipients[recipientEmail] = 0;

        return Task.FromResult(true);
    }

    public bool WasNotified(string recipientEmail) => _recipients.ContainsKey(recipientEmail);

    public async Task<bool> WaitForNoticeAsync(string recipientEmail)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (WasNotified(recipientEmail))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class ResendConfirmationEmailSender(
    HttpClient httpClient,
    IOptions<EmailConfirmationOptions> options,
    ILogger<ResendConfirmationEmailSender> logger) : IConfirmationEmailSender
{
    private const string SendEmailPath = "emails";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031 // El envío se reintenta desde el outbox: un fallo se registra y se devuelve como no entregado.
        try
        {
            return await PostAsync(recipientEmail, rawToken, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Confirmation email delivery failed and will be retried");
            return false;
        }
#pragma warning restore CA1031
    }

    private async Task<bool> PostAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
        EmailConfirmationOptions settings = options.Value;

        using HttpRequestMessage request = new(HttpMethod.Post, SendEmailPath)
        {
            Content = JsonContent.Create(
                ResendEmailRequest.Confirmation(settings, recipientEmail, rawToken), options: SerializerOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Resend rejected the confirmation email with status {StatusCode}", (int)response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }
}

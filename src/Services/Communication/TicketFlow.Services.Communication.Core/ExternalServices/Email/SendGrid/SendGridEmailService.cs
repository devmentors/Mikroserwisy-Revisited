using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Services.Communication.Core.ExternalServices.Email.SendGrid;

public sealed class SendGridEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _defaultFromEmail = "noreply@ticketflow.com";

    private const int MaxRetries = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);

    public SendGridEmailService(HttpClient httpClient, ILogger<SendGridEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending email to {To}", message.To);
        var request = TranslateToSendGridModel(message);
        return await SendWithSmartRetry(request, cancellationToken);
    }

    private async Task<EmailSendResult> SendWithSmartRetry(
        SendGridRequest request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/mail/send", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return await HandleSuccess(response, cancellationToken);

                var result = (int)response.StatusCode switch
                {
                    401 => HandleUnauthorized(),
                    400 => HandleBadRequest(),
                    429 => await HandleQuotaExceeded(response, attempt, cancellationToken),
                    503 => await HandleServiceUnavailable(attempt, cancellationToken),
                    _ => HandleUnknownError((int)response.StatusCode)
                };

                if (result is not null)
                    return result;
            }
            catch (Exception ex)
            {
                var result = await HandleException(ex, attempt, cancellationToken);
                if (result is not null)
                    return result;
            }
        }

        return Error(EmailServiceErrorCode.UnknownError, "Unexpected error in retry logic");
    }

    private async Task<EmailSendResult> HandleSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        var sgResponse = await response.Content.ReadFromJsonAsync<SendGridResponse>(ct);
        _logger.LogInformation("Email sent, MessageId: {MessageId}", sgResponse?.MessageId);
        return new EmailSendResult(true, sgResponse?.MessageId, EmailServiceErrorCode.Success, null);
    }

    private EmailSendResult HandleUnauthorized()
    {
        _logger.LogError("SendGrid 401 -> ConfigurationError");
        return Error(EmailServiceErrorCode.ConfigurationError, "Invalid credentials");
    }

    private EmailSendResult HandleBadRequest()
    {
        _logger.LogError("SendGrid 400 -> ValidationError");
        return Error(EmailServiceErrorCode.ValidationError, "Invalid email format");
    }

    private EmailSendResult HandleUnknownError(int statusCode)
    {
        _logger.LogError("SendGrid {StatusCode} -> UnknownError", statusCode);
        return Error(EmailServiceErrorCode.UnknownError, "Unexpected error");
    }

    private async Task<EmailSendResult?> HandleQuotaExceeded(
        HttpResponseMessage response, int attempt, CancellationToken ct)
    {
        if (attempt < MaxRetries)
        {
            var retryAfter = GetRetryAfterSeconds(response);
            _logger.LogWarning("Quota exceeded. Retry {Attempt}/{Max} after {Seconds}s", attempt + 1, MaxRetries, retryAfter);
            await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
            return null;
        }
        _logger.LogError("SendGrid 429 -> QuotaExceeded");
        return Error(EmailServiceErrorCode.QuotaExceeded, "Quota exceeded");
    }

    private async Task<EmailSendResult?> HandleServiceUnavailable(int attempt, CancellationToken ct)
    {
        if (attempt < MaxRetries)
        {
            var delay = BaseDelay * Math.Pow(2, attempt);
            _logger.LogWarning("Service unavailable. Retry {Attempt}/{Max} after {Delay}ms", attempt + 1, MaxRetries, delay.TotalMilliseconds);
            await Task.Delay(delay, ct);
            return null;
        }
        _logger.LogError("SendGrid 503 -> TemporarilyUnavailable");
        return Error(EmailServiceErrorCode.TemporarilyUnavailable, "Service unavailable");
    }

    private async Task<EmailSendResult?> HandleException(Exception ex, int attempt, CancellationToken ct)
    {
        _logger.LogError(ex, "Exception on attempt {Attempt}", attempt + 1);
        if (attempt < MaxRetries)
        {
            await Task.Delay(BaseDelay * Math.Pow(2, attempt), ct);
            return null;
        }
        return Error(EmailServiceErrorCode.UnknownError, $"Failed after {MaxRetries} retries: {ex.Message}");
    }

    private static EmailSendResult Error(EmailServiceErrorCode code, string message) =>
        new(false, null, code, message);

    private static int GetRetryAfterSeconds(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Retry-After", out var values) && int.TryParse(values.First(), out var s) ? s : 60;

    private SendGridRequest TranslateToSendGridModel(EmailMessage message) => new()
    {
        From = new EmailAddress { Email = message.From ?? _defaultFromEmail, Name = "TicketFlow" },
        Subject = message.Subject,
        Personalizations =
        [
            new Personalization
            {
                To = [new EmailAddress { Email = message.To }],
                Cc = message.Cc?.Select(cc => new EmailAddress { Email = cc }).ToList(),
                Bcc = message.Bcc?.Select(bcc => new EmailAddress { Email = bcc }).ToList()
            }
        ],
        Content = [new Content { Type = "text/plain", Value = message.Body }],
        Priority = message.Priority >= EmailPriority.High ? 1 : null,
        Categories = message.Priority switch
        {
            EmailPriority.Urgent => ["urgent", "high-priority"],
            EmailPriority.High => ["high-priority"],
            _ => null
        }
    };
}

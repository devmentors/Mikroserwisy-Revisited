using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Services.SLA.Core.Http.Billing;

internal class BillingClient : IBillingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BillingClient> _logger;

    public BillingClient(HttpClient httpClient, ILogger<BillingClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CustomerPaymentStandingDto?> GetPaymentStandingAsync(
        string domain, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking payment standing for domain: {Domain}", domain);

            var response = await _httpClient.GetAsync(
                $"/customers/{Uri.EscapeDataString(domain)}/payment-standing",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get payment standing for {Domain}: {StatusCode}",
                    domain, response.StatusCode);
                return null;
            }

            var standing = await response.Content
                .ReadFromJsonAsync<CustomerPaymentStandingDto>(cancellationToken);

            _logger.LogDebug(
                "Payment standing for {Domain}: InGoodStanding={Good}, DaysOverdue={Days}",
                domain, standing?.InGoodStanding, standing?.DaysOverdue);

            return standing;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "BillingIntegration service unavailable for {Domain}. Using contracted tier.",
                domain);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning(
                "Timeout getting payment standing for {Domain}. Using contracted tier.",
                domain);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting payment standing for {Domain}. Using contracted tier.",
                domain);
            return null;
        }
    }
}

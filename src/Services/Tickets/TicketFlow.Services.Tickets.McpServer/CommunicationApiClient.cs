using System.Text;
using System.Text.Json;

namespace TicketFlow.Services.Tickets.McpServer;

public class CommunicationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommunicationApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public CommunicationApiClient(HttpClient httpClient, ILogger<CommunicationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(
        Guid recipientUserId,
        string recipientEmail,
        string title,
        string content,
        CancellationToken ct = default)
    {
        try
        {
            var messagePayload = new
            {
                RecipentUserId = recipientUserId,
                RecipentEmail = recipientEmail,
                Title = title,
                Content = content,
                Timestamp = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(messagePayload, JsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/messages", httpContent, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {Email}", recipientEmail);
            return false;
        }
    }
}

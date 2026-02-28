using System.Net.Http.Json;
using System.Text.Json;

namespace TicketFlow.Agents.KnowledgeBase.Services;

/// <summary>
/// MCP client for searching resolved tickets.
/// Calls Tickets MCP Server to get historical solutions.
///
/// NOTE: This is a SIMPLIFIED client using direct REST calls.
/// For full MCP JSON-RPC protocol with session management,
/// see EscalationAgent's TicketsMcpClient (with thread-safe initialization).
/// </summary>
public class TicketsMcpClient : ITicketsMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TicketsMcpClient> _logger;
    private readonly string _mcpUrl;

    public TicketsMcpClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TicketsMcpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mcpUrl = configuration["KnowledgeBase:TicketsMcpUrl"]
            ?? "http://localhost:5401";
    }

    public async Task<List<ResolvedTicketResult>> SearchResolvedTicketsAsync(
        string query,
        string? category = null,
        int limit = 3)
    {
        _logger.LogInformation(
            "Searching resolved tickets - Query: {Query}, Category: {Category}",
            query, category ?? "all");

        try
        {
            var jsonRpcRequest = new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString(),
                method = "tools/call",
                @params = new
                {
                    name = "search_resolved_tickets",
                    arguments = new
                    {
                        query,
                        category,
                        limit
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_mcpUrl}/mcp",
                jsonRpcRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MCP server returned status {Status}", response.StatusCode);
                return [];
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("MCP Response: {Response}", responseContent);

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                _logger.LogWarning("MCP error: {Error}", error.GetRawText());
                return [];
            }

            if (!root.TryGetProperty("result", out var result))
            {
                _logger.LogWarning("No result in MCP response");
                return [];
            }

            if (!result.TryGetProperty("content", out var content))
            {
                _logger.LogWarning("No content in MCP result");
                return [];
            }

            var contentArray = content.EnumerateArray().ToList();
            if (contentArray.Count == 0)
            {
                return [];
            }

            var textContent = contentArray[0].GetProperty("text").GetString();
            if (string.IsNullOrEmpty(textContent))
            {
                return [];
            }

            var searchResponse = JsonSerializer.Deserialize<SearchResolvedTicketsResponse>(
                textContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (searchResponse?.Results == null)
            {
                _logger.LogWarning("No results in search response");
                return [];
            }

            _logger.LogInformation("Found {Count} resolved tickets", searchResponse.Results.Count);
            return searchResponse.Results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching resolved tickets");
            return [];
        }
    }

    private record SearchResolvedTicketsResponse
    {
        public List<ResolvedTicketResult> Results { get; init; } = [];
        public string? Summary { get; init; }
    }
}

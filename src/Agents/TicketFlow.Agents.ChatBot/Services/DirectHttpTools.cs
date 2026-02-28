using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Agents.ChatBot.Services;

internal static class DirectHttpTools
{
    public static List<AITool> Create(string ticketsApiBaseUrl, ILogger logger)
    {
        logger.LogInformation("DirectHttpTools targeting: {BaseUrl}", ticketsApiBaseUrl);
        var httpClient = new HttpClient { BaseAddress = new Uri(ticketsApiBaseUrl) };

        return
        [
            AIFunctionFactory.Create(async (string? status, int page = 1, int limit = 10) =>
            {
                var query = $"?page={page}&limit={limit}";
                var validStatuses = new[] { "BeforeQualification", "Qualified", "Resolved", "Blocked", "WaitingForCapacity" };
                if (!string.IsNullOrEmpty(status) && validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                    query += $"&status={status}";
                var url = $"/tickets/{query}";

                try
                {
                    logger.LogInformation("DirectHTTP GET {Url}", url);
                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        logger.LogWarning("DirectHTTP {Url} returned {Status}: {Body}", url, (int)response.StatusCode, body);
                        return $"Error: HTTP {(int)response.StatusCode} - {body}";
                    }
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DirectHTTP {Url} failed", url);
                    return $"Error: {ex.Message}";
                }
            }, new AIFunctionFactoryOptions
            {
                Name = "list_tickets",
                Description = "Returns list of tickets from the Tickets API as raw JSON"
            }),

            AIFunctionFactory.Create(async (string ticketId) =>
            {
                var url = $"/tickets/{ticketId}/";
                try
                {
                    logger.LogInformation("DirectHTTP GET {Url}", url);
                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return $"Ticket {ticketId} not found";
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DirectHTTP {Url} failed", url);
                    return $"Error: {ex.Message}";
                }
            }, new AIFunctionFactoryOptions
            {
                Name = "get_ticket",
                Description = "Returns ticket details by ID as raw JSON"
            }),

            AIFunctionFactory.Create(async () =>
            {
                try
                {
                    logger.LogInformation("DirectHTTP GET /agents");
                    var response = await httpClient.GetAsync("/agents");
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        logger.LogWarning("DirectHTTP /agents returned {Status}: {Body}", (int)response.StatusCode, body);
                        return $"Error: HTTP {(int)response.StatusCode} - {body}";
                    }
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DirectHTTP /agents failed");
                    return $"Error: {ex.Message}";
                }
            }, new AIFunctionFactoryOptions
            {
                Name = "list_agents",
                Description = "Returns list of support agents as raw JSON"
            })
        ];
    }
}

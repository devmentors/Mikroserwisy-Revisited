using ModelContextProtocol.Client;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Shared.Services;

internal sealed class McpClientFactory : IMcpClientFactory
{
    public async Task<McpClient> CreateClientAsync(string serverUrl, UserContext userContext, TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(30) };

        if (userContext.UserId != null)
            httpClient.DefaultRequestHeaders.Add("X-User-Id", userContext.UserId);

        httpClient.DefaultRequestHeaders.Add("X-User-Role", userContext.Role);
        httpClient.DefaultRequestHeaders.Add("X-User-Email", userContext.Email);

        if (userContext.AgentId != null)
            httpClient.DefaultRequestHeaders.Add("X-Agent-Id", userContext.AgentId);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(serverUrl),
                ConnectionTimeout = TimeSpan.FromSeconds(5)
            },
            httpClient,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }
}

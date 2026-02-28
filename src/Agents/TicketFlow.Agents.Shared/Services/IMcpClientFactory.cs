using ModelContextProtocol.Client;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Shared.Services;

public interface IMcpClientFactory
{
    Task<McpClient> CreateClientAsync(string serverUrl, UserContext userContext, TimeSpan? timeout = null);
}

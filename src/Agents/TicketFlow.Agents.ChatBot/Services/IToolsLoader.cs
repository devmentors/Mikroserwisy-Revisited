using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.ChatBot.Services;

public interface IToolsLoader
{
    Task<ToolsLoadResult> LoadToolsAsync(UserContext userContext, CancellationToken ct = default);
    Task<IReadOnlyList<ToolInfo>> GetAvailableToolsInfoAsync(UserContext userContext, CancellationToken ct = default);
}

public record ToolsLoadResult(List<McpClient> McpClients, List<AITool> Tools) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        foreach (var client in McpClients)
        {
            await client.DisposeAsync();
        }
    }
}

public record ToolInfo(string Name, string? Description, string Server);

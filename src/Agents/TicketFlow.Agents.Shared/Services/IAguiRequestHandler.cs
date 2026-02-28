using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Shared.Services;

public interface IAguiRequestHandler
{
    Task HandleRequestAsync(
        HttpContext context,
        IChatClient chatClient,
        AITool[] tools,
        string systemPrompt,
        string requestBody,
        AguiHandlerOptions? options = null);
}

public record AguiHandlerOptions
{
    public int MaxIterationsPerRequest { get; init; } = 10;
}

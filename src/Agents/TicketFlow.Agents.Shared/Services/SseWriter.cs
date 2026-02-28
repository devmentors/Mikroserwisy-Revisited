using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Agents.Shared.Services;

internal sealed class SseWriter : ISseWriter
{
    private readonly ILogger<SseWriter> _logger;

    public SseWriter(ILogger<SseWriter> logger)
    {
        _logger = logger;
    }

    public async Task WriteEventAsync(HttpContext context, object eventData)
    {
        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventType = eventData.GetType().GetProperty("type")?.GetValue(eventData)?.ToString() ?? "unknown";
        _logger.LogDebug("Sending SSE event: {EventType}", eventType);

        await context.Response.WriteAsync($"data: {json}\n\n");
        await context.Response.Body.FlushAsync();
    }
}

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class SetTicketWaitingTool
{
    private readonly TicketsApiClient _client;

    public SetTicketWaitingTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "set_ticket_waiting")]
    [Description("Move ticket to waiting queue (WaitingForCapacity status) when no agents are available. Returns queue position. Available for: supervisor, admin, escalation_agent.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID) to move to waiting queue")]
        string ticketId,
        [Description("Reason why ticket is being queued (e.g., 'No agent capacity available')")]
        string reason,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var ticketGuid))
        {
            return JsonSerializer.Serialize(new { success = false, message = "Invalid ticket ID format" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return JsonSerializer.Serialize(new { success = false, message = "Reason is required" });
        }

        try
        {
            var result = await _client.SetTicketWaitingAsync(ticketGuid, reason, ct);

            if (!result.Success)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    ticketId,
                    message = result.Message
                });
            }
            
            await _client.AddClientNoteAsync(
                ticketGuid,
                $"Ticket queued for automatic assignment when agent capacity becomes available. Queue position: #{result.QueuePosition}",
                ct);

            return JsonSerializer.Serialize(new
            {
                success = true,
                ticketId,
                queuePosition = result.QueuePosition,
                message = $"Ticket moved to waiting queue at position #{result.QueuePosition}. Auto-assignment will occur when capacity opens up."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                ticketId,
                message = $"Error setting ticket waiting: {ex.Message}"
            });
        }
    }
}

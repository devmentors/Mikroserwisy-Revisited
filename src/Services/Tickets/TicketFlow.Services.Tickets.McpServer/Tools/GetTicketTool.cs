using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

// ANTI-PATTERN: Thin Wrapper
// Problems:
// 1. Raw JSON
// 2. No semantic context (what does this ticket need?)
// 3. No aggregation with related data (notes, agent info)
// Then -> LLM must interpret raw status codes

public class GetTicketTool
{
    private readonly TicketsApiClient _client;

    public GetTicketTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "get_ticket")]
    [Description("ANTI-PATTERN: Raw data response. Returns ticket details without semantic context.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")]
        string ticketId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var guid))
        {
            return JsonSerializer.Serialize(new { error = "Invalid ticket ID format. Expected GUID." });
        }

        var ticket = await _client.GetTicketAsync(guid, ct);

        if (ticket == null)
        {
            return JsonSerializer.Serialize(new { error = $"Ticket with ID {ticketId} not found." });
        }

        var result = new
        {
            id = ticket.Id,
            title = ticket.Title,
            description = ticket.Description,
            email = ticket.Email,
            status = ticket.Status,
            category = ticket.Category?.ToString(),
            severity = ticket.SeverityLevel?.ToString(),
            type = ticket.Type,
            assignedAgentUserId = ticket.AssignedAgentUserId?.ToString(),
            resolution = ticket.Resolution,
            createdAt = ticket.CreatedAt.ToString("O")
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

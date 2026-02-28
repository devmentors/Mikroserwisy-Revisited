using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class AddClientNoteTool
{
    private readonly TicketsApiClient _client;

    public AddClientNoteTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "add_client_note")]
    [Description("Add a communication note to a ticket. Notes are visible to the client and used for tracking conversation history. Available for: agent, supervisor, admin, escalation_agent.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")]
        string ticketId,
        [Description("Note content/message to add")]
        string content,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var ticketGuid))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid ticket ID format" });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Content cannot be empty" });
        }

        var success = await _client.AddClientNoteAsync(ticketGuid, content, ct);

        return JsonSerializer.Serialize(new
        {
            success,
            ticketId,
            message = success
                ? "Note added successfully"
                : "Failed to add note. Ticket may not exist."
        });
    }
}

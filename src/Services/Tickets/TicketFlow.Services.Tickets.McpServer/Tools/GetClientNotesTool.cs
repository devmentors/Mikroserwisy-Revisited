using System.ComponentModel;
using ModelContextProtocol.Server;
using TicketFlow.Services.Tickets.McpServer.Models;
using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class GetClientNotesTool
{
    private readonly TicketsApiClient _client;

    public GetClientNotesTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "get_client_notes")]
    [Description("Get conversation history for a ticket with context summary. Returns notes and conversation state to help craft appropriate response.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")]
        string ticketId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var ticketGuid))
        {
            return ErrorResponse.Create(
                "Invalid ticket ID format. Expected GUID.",
                "Verify the ticket ID and try again."
            ).ToJson();
        }

        var notes = await _client.GetClientNotesAsync(ticketGuid, ct);

        if (string.IsNullOrEmpty(notes))
        {
            return new ClientNotesResponse
            {
                TicketId = ticketGuid,
                Summary = "No communication history found for this ticket.",
                NoteCount = 0,
                ConversationState = "new",
                Suggestion = "This is a fresh ticket. You can start the conversation with an initial response."
            }.ToJson();
        }
        
        var noteLines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var noteCount = noteLines.Length;
        
        var (conversationState, suggestion) = noteCount switch
        {
            0 => ("new", "No prior communication. Start with a greeting and acknowledgment."),
            <= 2 => ("initial", "Early stage conversation. Review the notes and continue the dialogue."),
            <= 5 => ("ongoing", "Active conversation. Check the latest notes for context before responding."),
            _ => ("extended", "Long conversation history. Consider summarizing progress and next steps.")
        };
        
        var recentNotes = noteLines.TakeLast(3).ToList();

        return new ClientNotesResponse
        {
            TicketId = ticketGuid,
            Summary = $"Found {noteCount} note(s) in conversation history.",
            NoteCount = noteCount,
            ConversationState = conversationState,
            RecentNotes = recentNotes,
            FullHistory = notes,
            Suggestion = suggestion
        }.ToJson();
    }
}

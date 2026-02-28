using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class ApplyQualificationTool
{
    private readonly TicketsApiClient _client;

    public ApplyQualificationTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "apply_qualification")]
    [Description("Apply qualification (category and priority) to a ticket. This changes ticket status from BeforeQualification to Qualified, enabling agent assignment. Available for: supervisor, admin, escalation_agent.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")]
        string ticketId,
        [Description("Ticket type: Incident, Question, Task")]
        string ticketType,
        [Description("Severity level: Low, Medium, High, Critical")]
        string severityLevel,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var guid))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid ticket ID format" });
        }

        var validTypes = new[] { "Incident", "Question", "Task" };
        var validSeverities = new[] { "Low", "Medium", "High", "Critical" };

        if (!validTypes.Contains(ticketType, StringComparer.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Invalid ticket type. Must be one of: {string.Join(", ", validTypes)}"
            });
        }

        if (!validSeverities.Contains(severityLevel, StringComparer.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Invalid severity level. Must be one of: {string.Join(", ", validSeverities)}"
            });
        }

        var success = await _client.QualifyTicketAsync(guid, ticketType, severityLevel, ct);

        if (!success)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Failed to apply qualification. Ticket may already be qualified or resolved."
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            ticketId,
            ticketType,
            severityLevel,
            message = "Ticket qualified successfully. Status changed to Qualified. Agent assignment is now possible."
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}

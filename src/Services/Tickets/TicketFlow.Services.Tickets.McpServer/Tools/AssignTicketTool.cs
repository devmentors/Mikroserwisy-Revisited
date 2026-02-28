using System.ComponentModel;
using System.Text.Json;

using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class AssignTicketTool
{
    private readonly TicketsApiClient _client;

    public AssignTicketTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "assign_ticket")]
    [Description("Assign ticket to agent using load balancing logic. Finds agents with matching specialization and selects one with lowest workload. Available for: supervisor, admin, escalation_agent.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID) to assign")]
        string ticketId,
        [Description("Ticket category: General=0, Technical=1, Billing=2, Other=3")]
        int category,
        [Description("Ticket priority (SeverityLevel): Low=0, Medium=1, High=2, Critical=3")]
        int priority,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var ticketGuid))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                reasoning = "Invalid ticket ID format"
            });
        }

        try
        {
            var agents = await _client.ListAgentsAsync(ct);

            if (agents.Length == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    reasoning = "No agents available in the system"
                });
            }
            
            var categoryName = category switch
            {
                0 => "General",
                1 => "Technical",
                2 => "Billing",
                3 => "Other",
                _ => "Other"
            };
            
            var matchingAgents = agents
                .Where(a => a.Specializations?.Contains(categoryName, StringComparer.OrdinalIgnoreCase) == true)
                .ToList();

            if (matchingAgents.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    reasoning = $"No agents found with {categoryName} specialization"
                });
            }

            // Not at full capacity
            var availableAgents = matchingAgents
                .Where(a => a.AssignedTicketCount < a.MaxTickets)
                .ToList();

            if (availableAgents.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    reasoning = $"No agents with capacity available for {categoryName} category. All {matchingAgents.Count} matching agents are at full capacity."
                });
            }

            // Minimum workload
            var selectedAgent = availableAgents
                .OrderBy(a => a.AssignedTicketCount)
                .First();
            
            var assignResult = await _client.AssignAgentAsync(ticketGuid, selectedAgent.Id, ct);

            if (!assignResult.Success)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    reasoning = $"Failed to assign ticket to agent {selectedAgent.FullName}: {assignResult.ErrorBody}"
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                assignedToAgentId = selectedAgent.Id.ToString(),
                assignedToAgentName = selectedAgent.FullName,
                reasoning = $"Assigned to {selectedAgent.FullName} (workload: {selectedAgent.AssignedTicketCount + 1}/{selectedAgent.MaxTickets})"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                reasoning = $"Error during assignment: {ex.Message}"
            });
        }
    }
}

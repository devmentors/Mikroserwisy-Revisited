using System.ComponentModel;
using ModelContextProtocol.Server;
using TicketFlow.Services.Tickets.McpServer.Models;
using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

// GOOD PRACTICE: Cognitive Translation Layer
// 1. Returns actual ticket data so the LLM can reason about it
// 2. Adds pre-computed next steps so the LLM knows what to do
// 3. Token-efficient - concise ticket summaries, not full DTOs

public class ListTicketsGoodTool
{
    private readonly TicketsApiClient _client;

    public ListTicketsGoodTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "list_tickets_good")]
    [Description("BEST PRACTICE: Semantic summary. Returns ticket overview with context - what needs attention, suggested actions.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket status filter: BeforeQualification, Qualified, Resolved, Blocked, WaitingForCapacity")]
        string? status = null,
        [Description("Filter by assigned agent ID (GUID)")]
        string? agentId = null,
        [Description("Page number (default 1)")]
        int page = 1,
        [Description("Results per page (default 10, max 50)")]
        int limit = 10,
        CancellationToken ct = default)
    {
        Guid? parsedAgentId = null;
        if (!string.IsNullOrEmpty(agentId) && Guid.TryParse(agentId, out var guid))
        {
            parsedAgentId = guid;
        }

        var tickets = await _client.ListTicketsAsync(parsedAgentId, status, page, Math.Min(limit, 50), ct);

        var ticketSummaries = tickets.Data
            .Select(t => new TicketSummary(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.SeverityLevel?.ToString(),
                t.AgentId?.ToString(),
                t.CreatedAt
            ))
            .ToList();

        var pendingCount = tickets.Data.Count(t => t.Status.ToString() == "BeforeQualification");
        var unassignedCount = tickets.Data.Count(t => t.Status.ToString() == "Qualified" && t.AgentId == null);
        var urgentCount = tickets.Data.Count(t => t.SeverityLevel?.ToString() is "Urgent" or "High");

        var nextSteps = new List<string>();

        if (pendingCount > 0)
            nextSteps.Add($"Use qualify_ticket to categorize {pendingCount} pending ticket(s)");

        if (unassignedCount > 0)
            nextSteps.Add($"Use assign_ticket to assign {unassignedCount} qualified but unassigned ticket(s)");

        if (urgentCount > 0)
            nextSteps.Add($"Prioritize {urgentCount} high-priority ticket(s)");

        if (nextSteps.Count == 0)
            nextSteps.Add("All tickets are processed. Monitor for new incoming tickets.");

        var response = new ListTicketsResponse
        {
            Summary = $"Found {tickets.TotalCount} tickets total (showing {ticketSummaries.Count} on page {page}).",
            Tickets = ticketSummaries,
            NextSteps = nextSteps,
            Pagination = new PaginationInfo(
                page,
                limit,
                (int)Math.Ceiling(tickets.TotalCount / (double)limit)
            )
        };

        return response.ToJson();
    }
}

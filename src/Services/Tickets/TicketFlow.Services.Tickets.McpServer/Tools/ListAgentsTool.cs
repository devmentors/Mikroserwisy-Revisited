using System.ComponentModel;
using ModelContextProtocol.Server;
using TicketFlow.Services.Tickets.McpServer.Models;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class ListAgentsTool
{
    private readonly TicketsApiClient _client;

    public ListAgentsTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "list_agents")]
    [Description("List support agents with availability summary. Returns capacity info and specialization breakdown. Use before assign_ticket to understand team capacity.")]
    public async Task<string> ExecuteAsync(CancellationToken ct = default)
    {
        var agents = await _client.ListAgentsAsync(ct);
        
        var availableAgents = agents.Where(a => a.AssignedTicketCount < a.MaxTickets).ToList();
        var atCapacityAgents = agents.Where(a => a.AssignedTicketCount >= a.MaxTickets).ToList();
        var totalCapacity = agents.Sum(a => a.MaxTickets);
        var usedCapacity = agents.Sum(a => a.AssignedTicketCount);
        
        var specializationCoverage = agents
            .SelectMany(a => a.Specializations ?? [])
            .GroupBy(s => s)
            .ToDictionary(
                g => g.Key,
                g => agents.Count(a => a.Specializations?.Contains(g.Key) == true && a.AssignedTicketCount < a.MaxTickets)
            );
        
        var recommendedAgents = new Dictionary<string, RecommendedAgent>();
        foreach (var spec in specializationCoverage.Keys)
        {
            var bestAgent = agents
                .Where(a => a.Specializations?.Contains(spec) == true && a.AssignedTicketCount < a.MaxTickets)
                .OrderBy(a => a.AssignedTicketCount)
                .FirstOrDefault();

            if (bestAgent != null)
            {
                recommendedAgents[spec] = new RecommendedAgent(
                    bestAgent.Id,
                    bestAgent.FullName,
                    $"{bestAgent.AssignedTicketCount}/{bestAgent.MaxTickets}"
                );
            }
        }

        // Semantic summary
        var summaryParts = new List<string>
        {
            $"{agents.Length} agents total",
            $"{availableAgents.Count} with capacity",
            $"{atCapacityAgents.Count} at full capacity"
        };
        
        var warnings = new List<string>();

        if (usedCapacity > totalCapacity * 0.8)
            warnings.Add("Team is at >80% capacity");

        warnings.AddRange(
            specializationCoverage
                .Where(kvp => kvp.Value == 0)
                .Select(kvp => $"No available agents for {kvp.Key} category")
        );

        var response = new ListAgentsResponse
        {
            Summary = string.Join(", ", summaryParts),
            Capacity = new CapacityInfo(
                totalCapacity,
                usedCapacity,
                totalCapacity - usedCapacity,
                totalCapacity > 0 ? Math.Round((double)usedCapacity / totalCapacity * 100, 1) : 0
            ),
            AvailableBySpecialization = specializationCoverage,
            RecommendedAgents = recommendedAgents.Count > 0 ? recommendedAgents : null,
            Warnings = warnings.Count > 0 ? warnings : null,
            Agents = agents.Select(a => new AgentInfo(
                a.Id,
                a.FullName,
                $"{a.AssignedTicketCount}/{a.MaxTickets}",
                a.AssignedTicketCount < a.MaxTickets,
                a.Specializations
            )).ToList()
        };

        return response.ToJson();
    }
}

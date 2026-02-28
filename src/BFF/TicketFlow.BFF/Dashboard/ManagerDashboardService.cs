using TicketFlow.BFF.Http.Aggregation;
using TicketFlow.BFF.Http.PersonalInfoVault;
using TicketFlow.BFF.Http.Sla;
using TicketFlow.BFF.Http.Tickets;

namespace TicketFlow.BFF.Dashboard;

public class ManagerDashboardService
{
    private readonly ITicketsClient _ticketsClient;
    private readonly ISlaClient _slaClient;
    private readonly IPersonalInfoVaultClient _vaultClient;
    private readonly IAggregationClient _aggregationClient;

    public ManagerDashboardService(
        ITicketsClient ticketsClient,
        ISlaClient slaClient,
        IPersonalInfoVaultClient vaultClient,
        IAggregationClient aggregationClient)
    {
        _ticketsClient = ticketsClient;
        _slaClient = slaClient;
        _vaultClient = vaultClient;
        _aggregationClient = aggregationClient;
    }

    public async Task<ManagerDashboardDto> GetDashboard(CancellationToken cancellationToken)
    {
        // 1. SCATTER: Get tickets and agents in parallel
        var ticketsTask = _ticketsClient.GetTickets(1, 100, cancellationToken);
        var agentsTask = _ticketsClient.GetAgents(cancellationToken);

        await Task.WhenAll(ticketsTask, agentsTask);

        var ticketsResult = ticketsTask.Result;
        var agents = agentsTask.Result;
        var agentsById = agents.ToDictionary(a => a.Id, a => a);

        if (ticketsResult?.Data is null || ticketsResult.Data.Count == 0)
        {
            return new ManagerDashboardDto(
                [],
                new DashboardSummaryDto(0, 0, 0, 0, 0, 0, 0));
        }

        // 2. SCATTER: Get SLA info for each ticket in parallel
        var slaTasksByTicketId = ticketsResult.Data
            .ToDictionary(
                t => t.Id,
                t => GetSlaForTicket(t, cancellationToken));

        await Task.WhenAll(slaTasksByTicketId.Values);

        // 3. GATHER: Aggregate ticket + SLA + Agent data
        var ticketsWithSla = ticketsResult.Data.Select(ticket =>
        {
            var slaInfo = slaTasksByTicketId[ticket.Id].Result;
            var agentInfo = ticket.AgentId.HasValue && agentsById.TryGetValue(ticket.AgentId.Value.ToString(), out var agent)
                ? new AgentInfoDto(agent.Id, agent.FullName, agent.Position, agent.AvatarUrl)
                : null;

            return new TicketWithSlaDto(
                ticket.Id,
                ticket.Name,
                ticket.Email,
                ticket.Title,
                ticket.Category,
                ticket.Status,
                ticket.SeverityLevel,
                ticket.CreatedAt,
                agentInfo,
                slaInfo);
        }).ToList();

        // 4. Calculate summary
        var summary = CalculateSummary(ticketsWithSla);

        return new ManagerDashboardDto(ticketsWithSla, summary);
    }

    public async Task<List<TicketProjectionDto>> SearchProjectionsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetPersonTokensAsync(query, cancellationToken);
        if (tokens.Count == 0)
        {
            return [];
        }

        return await _aggregationClient.GetProjectionsByTokensAsync(tokens, cancellationToken);
    }

    public async Task<TicketsListDto> SearchTicketsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetPersonTokensAsync(query, cancellationToken);
        if (tokens.Count == 0)
        {
            return new TicketsListDto([], 0);
        }

        return await _ticketsClient.GetTicketsByTokensAsync(tokens, cancellationToken);
    }

    private async Task<SlaStatusDto?> GetSlaForTicket(TicketEntryDto ticket, CancellationToken cancellationToken)
    {
        var serviceType = MapTicketTypeToServiceType(ticket.Type);
        if (serviceType is null) return null;

        var slaData = await _slaClient.GetDeadlineReminders(serviceType, ticket.Id, cancellationToken);
        if (slaData is null) return null;

        return new SlaStatusDto(
            slaData.DeadlineDateUtc,
            slaData.DeadlineMet,
            slaData.ServiceCompleted,
            CalculateTimeRemaining(slaData.DeadlineDateUtc, slaData.ServiceCompleted),
            DetermineSlaState(slaData));
    }

    private async Task<List<string>> GetPersonTokensAsync(string query, CancellationToken cancellationToken)
    {
        var searchResult = await _vaultClient.SearchAsync(query, 50, cancellationToken);
        return searchResult.Results.Select(r => r.PersonToken).ToList();
    }

    private static string? MapTicketTypeToServiceType(string? ticketType)
    {
        return ticketType?.ToLowerInvariant() switch
        {
            "incident" => "IncidentTicket",
            "question" => "QuestionTicket",
            _ => "IncidentTicket"
        };
    }

    private static TimeRemainingDto? CalculateTimeRemaining(DateTimeOffset deadline, bool serviceCompleted)
    {
        if (serviceCompleted)
            return null;

        var remaining = deadline - DateTimeOffset.UtcNow;
        var isOverdue = remaining.TotalSeconds <= 0;
        var absRemaining = isOverdue ? remaining.Negate() : remaining;

        return new TimeRemainingDto(
            Days: (int)absRemaining.TotalDays,
            Hours: absRemaining.Hours,
            Minutes: absRemaining.Minutes,
            TotalMinutes: (int)absRemaining.TotalMinutes,
            IsOverdue: isOverdue);
    }

    private static string DetermineSlaState(DeadlineRemindersDto sla)
    {
        if (sla.ServiceCompleted)
        {
            return sla.DeadlineMet switch
            {
                true => "CompletedLate",
                false => "CompletedOnTime",
                null => "Completed"
            };
        }

        if (sla.DeadlineMet == false)
            return "Breached";

        var remaining = sla.DeadlineDateUtc - DateTimeOffset.UtcNow;

        if (remaining.TotalHours <= 1)
            return "AtRisk";

        return "OnTrack";
    }

    private static DashboardSummaryDto CalculateSummary(List<TicketWithSlaDto> tickets)
    {
        var totalTickets = tickets.Count;
        var openTickets = tickets.Count(t => !IsClosedStatus(t.Status));

        var breachedSla = tickets.Count(t => t.SlaStatus?.SlaState == "Breached");
        var atRiskSla = tickets.Count(t => t.SlaStatus?.SlaState == "AtRisk");
        var onTrackSla = tickets.Count(t => t.SlaStatus?.SlaState == "OnTrack");
        var completedOnTime = tickets.Count(t => t.SlaStatus?.SlaState == "CompletedOnTime");
        var completedLate = tickets.Count(t => t.SlaStatus?.SlaState == "CompletedLate");

        return new DashboardSummaryDto(
            totalTickets,
            openTickets,
            breachedSla,
            atRiskSla,
            onTrackSla,
            completedOnTime,
            completedLate);
    }

    private static bool IsClosedStatus(string status)
    {
        return status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Closed", StringComparison.OrdinalIgnoreCase);
    }
}

using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Tickets.McpServer.Models;

/// <summary>
/// Response for list_tickets_good tool with semantic context.
/// </summary>
public sealed record ListTicketsResponse : SemanticResponse
{
    public required IReadOnlyList<TicketSummary> Tickets { get; init; }
    public required PaginationInfo Pagination { get; init; }
}

public sealed record TicketSummary(
    string Id,
    string Title,
    string Status,
    string? Severity,
    string? AgentId,
    DateTimeOffset CreatedAt
);

public sealed record PaginationInfo(int Page, int Limit, int TotalPages);

/// <summary>
/// Response for list_agents tool with capacity information.
/// </summary>
public sealed record ListAgentsResponse : SemanticResponse
{
    public required CapacityInfo Capacity { get; init; }
    public required Dictionary<string, int> AvailableBySpecialization { get; init; }
    public Dictionary<string, RecommendedAgent>? RecommendedAgents { get; init; }
    public required IReadOnlyList<AgentInfo> Agents { get; init; }
}

public sealed record CapacityInfo(
    int Total,
    int Used,
    int Available,
    double UtilizationPercent
);

public sealed record RecommendedAgent(
    Guid AgentId,
    string Name,
    string CurrentLoad
);

public sealed record AgentInfo(
    Guid Id,
    string Name,
    string Load,
    bool Available,
    IReadOnlyList<string>? Specializations
);

/// <summary>
/// Response for get_client_notes tool with conversation context.
/// </summary>
public sealed record ClientNotesResponse : SemanticResponse
{
    public required Guid TicketId { get; init; }
    public required int NoteCount { get; init; }
    public required string ConversationState { get; init; }
    public IReadOnlyList<string>? RecentNotes { get; init; }
    public string? FullHistory { get; init; }
}

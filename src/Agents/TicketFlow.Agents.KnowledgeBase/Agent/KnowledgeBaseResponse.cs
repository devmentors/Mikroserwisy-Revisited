using TicketFlow.Agents.KnowledgeBase.Services;

namespace TicketFlow.Agents.KnowledgeBase.Agent;

public record KnowledgeBaseResponse
{
    public required bool Success { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }
    public required int StepCount { get; init; }
    public List<FaqSearchResult> FaqResults { get; init; } = [];
    public List<ResolvedTicketResult> ResolvedTickets { get; init; } = [];
}

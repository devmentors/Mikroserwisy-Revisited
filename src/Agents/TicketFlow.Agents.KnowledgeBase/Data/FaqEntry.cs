namespace TicketFlow.Agents.KnowledgeBase.Data;

public record FaqEntry
{
    public required string Id { get; init; }
    public required string Question { get; init; }
    public required string Answer { get; init; }
    public required string[] Keywords { get; init; }
    public required string Category { get; init; }
    public required int Priority { get; init; }
}

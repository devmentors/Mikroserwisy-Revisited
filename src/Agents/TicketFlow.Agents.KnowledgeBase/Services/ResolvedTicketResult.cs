namespace TicketFlow.Agents.KnowledgeBase.Services;

public record ResolvedTicketResult
{
    public required string TicketId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Resolution { get; init; }
    public required DateTime ResolvedAt { get; init; }
    public required int SimilarityScore { get; init; }
}

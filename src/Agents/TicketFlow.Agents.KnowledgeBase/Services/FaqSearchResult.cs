namespace TicketFlow.Agents.KnowledgeBase.Services;

public record FaqSearchResult
{
    public required string FaqId { get; init; }
    public required string Question { get; init; }
    public required string Answer { get; init; }
    public required string Category { get; init; }
    public required int Score { get; init; }
    public required List<string> MatchedKeywords { get; init; }
}

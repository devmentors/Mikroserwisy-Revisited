namespace TicketFlow.Agents.KnowledgeBase.Services;

public interface ITicketsMcpClient
{
    Task<List<ResolvedTicketResult>> SearchResolvedTicketsAsync(
        string query,
        string? category = null,
        int limit = 3);
}

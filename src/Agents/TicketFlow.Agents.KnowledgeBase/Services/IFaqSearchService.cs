namespace TicketFlow.Agents.KnowledgeBase.Services;

public interface IFaqSearchService
{
    List<FaqSearchResult> Search(string query, int maxResults = 3);
}

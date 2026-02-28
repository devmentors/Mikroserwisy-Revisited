namespace TicketFlow.BFF.Http.Tickets;

public interface ITicketsClient
{
    Task<TicketsListDto?> GetTickets(int page = 1, int limit = 100, CancellationToken cancellationToken = default);
    Task<List<AgentDto>> GetAgents(CancellationToken cancellationToken = default);
    Task<TicketsListDto> GetTicketsByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default);
}

namespace TicketFlow.Services.SLA.Core.Http.Tickets;

public interface ITicketsClient
{
    Task<TicketDto> GetTicketDetails(string ticketId, CancellationToken cancellationToken);
    Task<List<Guid>> GetSupervisorsUserIds(CancellationToken cancellationToken);
}
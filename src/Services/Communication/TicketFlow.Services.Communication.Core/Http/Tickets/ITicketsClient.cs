namespace TicketFlow.Services.Communication.Core.Http.Tickets;

public interface ITicketsClient
{
    Task<TicketDto> GetTicketDetails(string ticketId, CancellationToken cancellationToken);
}
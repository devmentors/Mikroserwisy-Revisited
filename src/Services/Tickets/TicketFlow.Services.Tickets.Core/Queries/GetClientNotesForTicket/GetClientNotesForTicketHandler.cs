using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.GetClientNotesForTicket;

public class GetClientNotesForTicketHandler(ITicketsRepository repository) : IQueryHandler<GetClientNotesForTicket, string>
{
    public async Task<string> HandleAsync(GetClientNotesForTicket query, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetAsync(query.TicketId, cancellationToken);

        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket with id {query.TicketId} was not found.");
        }

        return ticket.Notes ?? string.Empty;
    }
}
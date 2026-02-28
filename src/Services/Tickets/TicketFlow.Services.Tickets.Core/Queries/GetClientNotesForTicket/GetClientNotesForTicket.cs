using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.GetClientNotesForTicket;

public record GetClientNotesForTicket(Guid TicketId) : IQuery<string>;
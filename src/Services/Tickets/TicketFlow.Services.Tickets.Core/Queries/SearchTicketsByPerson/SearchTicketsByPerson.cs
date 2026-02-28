using TicketFlow.Services.Tickets.Core.Queries.ListTickets;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.SearchTicketsByPerson;

public record SearchTicketsByPerson(List<string> PersonTokens) : IQuery<TicketsListDto>;

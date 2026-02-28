using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.ListTickets;

public record ListTicketsQuery(Guid? AgentId, TicketStatus? Status, int Page, int Limit) : IQuery<TicketsListDto>;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.GetTicketDetails;

public record GetTicketDetailsQuery(Guid TicketId) : IQuery<TicketDetailsDto>;
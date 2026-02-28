using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.UnblockTicket;

public record UnblockTicket(Guid TicketId, string Reason) : ICommand;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.BlockTicket;

public record BlockTicket(Guid TicketId, string Reason) : ICommand;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.ResolveTicket;

public sealed record ResolveTicket(Guid TicketId, string Resolution) : ICommand;
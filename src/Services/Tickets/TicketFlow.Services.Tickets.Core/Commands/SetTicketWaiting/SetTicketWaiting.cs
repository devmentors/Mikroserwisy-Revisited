using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.SetTicketWaiting;

public sealed record SetTicketWaiting(Guid TicketId, string? Reason) : ICommand;

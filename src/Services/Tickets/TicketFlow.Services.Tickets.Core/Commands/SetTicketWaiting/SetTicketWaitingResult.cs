namespace TicketFlow.Services.Tickets.Core.Commands.SetTicketWaiting;

public sealed record SetTicketWaitingResult(bool Success, int QueuePosition, string Message);

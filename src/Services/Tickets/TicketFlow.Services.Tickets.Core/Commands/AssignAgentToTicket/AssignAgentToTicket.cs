using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.AssignAgentToTicket;

public sealed record AssignAgentToTicket(Guid TicketId, Guid AgentId) : ICommand;
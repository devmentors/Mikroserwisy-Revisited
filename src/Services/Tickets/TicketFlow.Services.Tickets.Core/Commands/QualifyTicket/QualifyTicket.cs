using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.QualifyTicket;

public sealed record QualifyTicket(Guid TicketId, TicketType TicketType, SeverityLevel SeverityLevel) : ICommand;
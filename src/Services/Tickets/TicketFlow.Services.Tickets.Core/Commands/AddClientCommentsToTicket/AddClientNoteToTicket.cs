using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.Commands.AddClientCommentsToTicket;

public record AddClientNoteToTicket(Guid TicketId, string NewNote) : ICommand;
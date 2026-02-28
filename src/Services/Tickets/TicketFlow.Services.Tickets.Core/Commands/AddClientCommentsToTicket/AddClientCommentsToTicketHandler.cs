using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Tickets.Core.Commands.AddClientCommentsToTicket;

public class AddClientCommentsToTicketHandler(ITicketsRepository repository) : ICommandHandler<AddClientNoteToTicket>
{
    public async Task HandleAsync(AddClientNoteToTicket command, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket with id {command.TicketId} was not found.");
        }
        
        ticket.AddClientNote(command.NewNote);
        await repository.UpdateAsync(ticket, cancellationToken);
    }
}
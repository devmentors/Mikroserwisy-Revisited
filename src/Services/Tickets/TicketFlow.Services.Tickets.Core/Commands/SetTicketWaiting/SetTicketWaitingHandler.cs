using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Tickets.Core.Commands.SetTicketWaiting;

internal sealed class SetTicketWaitingHandler : ICommandHandler<SetTicketWaiting>
{
    private readonly ITicketsRepository _repository;

    public SetTicketWaitingHandler(ITicketsRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(SetTicketWaiting command, CancellationToken cancellationToken = default)
    {
        var ticket = await _repository.GetAsync(command.TicketId, cancellationToken);
        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket {command.TicketId} not found");
        }

        // Calculate queue position
        var queuePosition = await _repository.CalculateQueuePositionAsync(command.TicketId, cancellationToken);
        var position = queuePosition ?? 1; // If not in queue yet, will be position 1

        // Set ticket to waiting status
        ticket.SetWaitingForCapacity(position, command.Reason ?? "No reason provided");

        await _repository.UpdateAsync(ticket, cancellationToken);
    }
}

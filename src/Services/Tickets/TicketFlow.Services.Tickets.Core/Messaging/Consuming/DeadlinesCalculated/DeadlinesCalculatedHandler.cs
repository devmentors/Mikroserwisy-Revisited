using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;

public class DeadlinesCalculatedHandler(ITicketsRepository repository, ILogger<DeadlinesCalculatedHandler> logger) : IMessageHandler<DeadlinesCalculated>
{
    public async Task HandleAsync(DeadlinesCalculated message, CancellationToken cancellationToken = default)
    {
        if (!message.ServiceType.Contains("Ticket"))
        {
            logger.LogInformation("Ignored deadline for service type that is not ticket: {type}", message.ServiceType);
            return;
        }

        var matchingTicket = await repository.GetAsync(Guid.Parse(message.ServiceSourceId), cancellationToken);

        if (matchingTicket is null)
        {
            throw new TicketFlowException($"Ticket not found for id: {message.ServiceSourceId}");
        }
        
        matchingTicket.SetCalculatedDeadline(message.DeadlineUtc);
        
        await repository.UpdateAsync(matchingTicket, cancellationToken);
    }
}
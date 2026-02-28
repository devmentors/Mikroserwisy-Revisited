using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class DeadlinesCalculatedHandler(
    ITicketProjectionRepository repository,
    ILogger<DeadlinesCalculatedHandler> logger) : IMessageHandler<DeadlinesCalculated>
{
    public async Task HandleAsync(DeadlinesCalculated message, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(message.ServiceSourceId, out var ticketId))
        {
            logger.LogWarning("Invalid ServiceSourceId: {ServiceSourceId}", message.ServiceSourceId);
            return;
        }

        var projection = await repository.GetByIdAsync(ticketId, cancellationToken);
        if (projection is null)
        {
            logger.LogWarning("TicketProjection {TicketId} not found for DeadlinesCalculated", ticketId);
            return;
        }

        projection.SlaDeadlineUtc = message.DeadlineUtc;
        projection.SlaBreached = false;
        await repository.UpdateAsync(projection, cancellationToken);
        logger.LogInformation("SLA deadline set for ticket {TicketId}: {DeadlineUtc}", ticketId, message.DeadlineUtc);
    }
}

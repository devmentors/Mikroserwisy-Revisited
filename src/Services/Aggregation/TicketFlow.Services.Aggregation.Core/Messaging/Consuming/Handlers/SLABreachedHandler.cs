using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class SLABreachedHandler(
    ITicketProjectionRepository repository,
    ILogger<SLABreachedHandler> logger) : IMessageHandler<SLABreached>
{
    public async Task HandleAsync(SLABreached message, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(message.ServiceSourceId, out var ticketId))
        {
            logger.LogWarning("Invalid ServiceSourceId: {ServiceSourceId}", message.ServiceSourceId);
            return;
        }

        var projection = await repository.GetByIdAsync(ticketId, cancellationToken);
        if (projection is null)
        {
            logger.LogWarning("TicketProjection {TicketId} not found for SLABreached", ticketId);
            return;
        }

        projection.SlaBreached = true;
        await repository.UpdateAsync(projection, cancellationToken);
        logger.LogInformation("SLA breached for ticket {TicketId}", ticketId);
    }
}

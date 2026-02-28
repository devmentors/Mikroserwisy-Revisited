using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class TicketResolvedHandler(
    ITicketProjectionRepository repository,
    ILogger<TicketResolvedHandler> logger) : IMessageHandler<TicketResolved>
{
    public async Task HandleAsync(TicketResolved message, CancellationToken cancellationToken = default)
    {
        var projection = await repository.GetByIdAsync(message.TicketId, cancellationToken);
        if (projection is null)
        {
            logger.LogWarning("TicketProjection {TicketId} not found for TicketResolved", message.TicketId);
            return;
        }

        if (message.Version <= projection.Version)
        {
            logger.LogDebug("Skipping stale TicketResolved (v{MessageVersion} <= v{ProjectionVersion})",
                message.Version, projection.Version);
            return;
        }

        projection.Status = "Resolved";
        projection.SlaServiceCompleted = true;
        projection.Version = message.Version;
        await repository.UpdateAsync(projection, cancellationToken);
        logger.LogInformation("Ticket {TicketId} resolved", message.TicketId);
    }
}

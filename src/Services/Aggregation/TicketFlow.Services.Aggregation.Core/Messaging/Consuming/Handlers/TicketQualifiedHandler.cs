using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Http;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class TicketQualifiedHandler(
    ITicketProjectionRepository repository,
    ITicketsClient ticketsClient,
    ILogger<TicketQualifiedHandler> logger) : IMessageHandler<TicketQualified>
{
    public async Task HandleAsync(TicketQualified message, CancellationToken cancellationToken = default)
    {
        var projection = await repository.GetByIdAsync(message.TicketId, cancellationToken);
        if (projection is null)
        {
            logger.LogWarning("TicketProjection {TicketId} not found for TicketQualified", message.TicketId);
            return;
        }

        if (message.Version <= projection.Version)
        {
            logger.LogDebug("Skipping stale TicketQualified (v{MessageVersion} <= v{ProjectionVersion})",
                message.Version, projection.Version);
            return;
        }

        try
        {
            var ticket = await ticketsClient.GetTicketAsync(message.TicketId, cancellationToken);
            if (ticket?.SeverityLevel is not null)
            {
                projection.SeverityLevel = ticket.SeverityLevel;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch ticket for severity: {TicketId}", message.TicketId);
        }

        projection.Status = "Qualified";
        projection.Version = message.Version;
        await repository.UpdateAsync(projection, cancellationToken);
        logger.LogInformation("Ticket {TicketId} qualified with severity {SeverityLevel}",
            message.TicketId, projection.SeverityLevel);
    }
}

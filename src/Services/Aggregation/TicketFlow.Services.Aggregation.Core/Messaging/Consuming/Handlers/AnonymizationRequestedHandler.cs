using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Messaging.Publishing;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class AnonymizationRequestedHandler(
    IMessagePublisher messagePublisher,
    ILogger<AnonymizationRequestedHandler> logger) : IMessageHandler<AnonymizationRequested>
{
    public async Task HandleAsync(AnonymizationRequested message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received AnonymizationRequested for person {PersonToken}", message.PersonToken);

        var completedEvent = new AnonymizationCompleted(
            message.RequestId,
            ServiceName: "aggregation",
            Success: true,
            ErrorMessage: null);

        await messagePublisher.PublishAsync(
            completedEvent,
            destination: AggregationTopologyInitializer.AggregationExchange,
            routingKey: "aggregation.anonymization.completed",
            cancellationToken: cancellationToken);

        logger.LogInformation("Anonymization completed for request {RequestId}", message.RequestId);
    }
}

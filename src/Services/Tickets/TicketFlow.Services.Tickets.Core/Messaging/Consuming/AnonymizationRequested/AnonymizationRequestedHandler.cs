using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing.Conventions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.AnonymizationRequested;

public class AnonymizationRequestedHandler(
    IMessagePublisher messagePublisher,
    ILogger<AnonymizationRequestedHandler> logger) : IMessageHandler<AnonymizationRequested>
{
    public async Task HandleAsync(AnonymizationRequested message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received AnonymizationRequested for person {PersonToken}", message.PersonToken);

        var completedEvent = new AnonymizationCompleted(
            message.RequestId,
            ServiceName: "tickets",
            Success: true,
            ErrorMessage: null);

        await messagePublisher.PublishAsync(
            completedEvent,
            destination: TicketsMessagePublisherConventionProvider.ExchangeName,
            routingKey: "tickets.anonymization.completed",
            cancellationToken: cancellationToken);

        logger.LogInformation("Anonymization completed for request {RequestId}", message.RequestId);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Anonymization.Core.Messaging.Consuming;
using TicketFlow.Services.Anonymization.Core.Messaging.Consuming.Handlers;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Anonymization.Core.Messaging;

public class AnonymizationConsumerService(
    IMessageConsumer messageConsumer,
    IServiceScopeFactory serviceScopeFactory,
    ISerializer serializer,
    ILogger<AnonymizationConsumerService> logger) : BackgroundService
{
    private const string AnonymizationEventsQueue = "anonymization-events-queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer.ConsumeNonGeneric(
            handleRawPayload: async messageData =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                await DispatchEventAsync(scope.ServiceProvider, messageData, stoppingToken);
            },
            queue: AnonymizationEventsQueue,
            cancellationToken: stoppingToken);

        logger.LogInformation("Started consuming events from anonymization-exchange");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task DispatchEventAsync(
        IServiceProvider serviceProvider,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var messageType = messageData.Type;
        logger.LogDebug("Received event: {MessageType}", messageType);

        switch (messageType)
        {
            case nameof(AnonymizationCompleted):
                var anonymizationCompleted = serializer.DeserializeBinary<AnonymizationCompleted>(messageData.Payload);
                if (anonymizationCompleted is not null)
                {
                    var handler = serviceProvider.GetRequiredService<AnonymizationCompletedHandler>();
                    await handler.HandleAsync(anonymizationCompleted, cancellationToken);
                }
                break;

            default:
                logger.LogWarning("Unknown event type: {MessageType}", messageType);
                break;
        }
    }
}

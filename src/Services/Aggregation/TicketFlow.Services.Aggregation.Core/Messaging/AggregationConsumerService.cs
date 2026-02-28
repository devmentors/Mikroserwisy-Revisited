using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Messaging.Consuming;
using TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Aggregation.Core.Messaging;

public class AggregationConsumerService(
    IMessageConsumer messageConsumer,
    IServiceScopeFactory serviceScopeFactory,
    ISerializer serializer,
    ILogger<AggregationConsumerService> logger) : BackgroundService
{
    private const string TicketEventsQueue = "aggregation-ticket-events";
    private const string SlaEventsQueue = "aggregation-sla-events";
    public const string AnonymizationQueue = "aggregation-anonymization-queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer.ConsumeNonGeneric(
            handleRawPayload: async messageData =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                await DispatchTicketEventAsync(scope.ServiceProvider, messageData, stoppingToken);
            },
            queue: TicketEventsQueue,
            cancellationToken: stoppingToken);

        await messageConsumer.ConsumeNonGeneric(
            handleRawPayload: async messageData =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                await DispatchSlaEventAsync(scope.ServiceProvider, messageData, stoppingToken);
            },
            queue: SlaEventsQueue,
            cancellationToken: stoppingToken);

        await messageConsumer.ConsumeNonGeneric(
            handleRawPayload: async messageData =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                await DispatchAnonymizationEventAsync(scope.ServiceProvider, messageData, stoppingToken);
            },
            queue: AnonymizationQueue,
            cancellationToken: stoppingToken);

        logger.LogInformation("Started consuming events from tickets-exchange, sla-exchange, and anonymization-exchange");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task DispatchTicketEventAsync(
        IServiceProvider serviceProvider,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var messageType = messageData.Type;
        logger.LogDebug("Received ticket event: {MessageType}", messageType);

        switch (messageType)
        {
            case nameof(TicketCreated):
                var ticketCreated = serializer.DeserializeBinary<TicketCreated>(messageData.Payload);
                if (ticketCreated is not null)
                {
                    var handler = serviceProvider.GetRequiredService<TicketCreatedHandler>();
                    await handler.HandleAsync(ticketCreated, cancellationToken);
                }
                break;

            case nameof(TicketQualified):
                var ticketQualified = serializer.DeserializeBinary<TicketQualified>(messageData.Payload);
                if (ticketQualified is not null)
                {
                    var handler = serviceProvider.GetRequiredService<TicketQualifiedHandler>();
                    await handler.HandleAsync(ticketQualified, cancellationToken);
                }
                break;

            case nameof(TicketResolved):
                var ticketResolved = serializer.DeserializeBinary<TicketResolved>(messageData.Payload);
                if (ticketResolved is not null)
                {
                    var handler = serviceProvider.GetRequiredService<TicketResolvedHandler>();
                    await handler.HandleAsync(ticketResolved, cancellationToken);
                }
                break;

            case nameof(AgentAssignedToTicket):
                var agentAssigned = serializer.DeserializeBinary<AgentAssignedToTicket>(messageData.Payload);
                if (agentAssigned is not null)
                {
                    var handler = serviceProvider.GetRequiredService<AgentAssignedHandler>();
                    await handler.HandleAsync(agentAssigned, cancellationToken);
                }
                break;

            default:
                logger.LogWarning("Unknown ticket event type: {MessageType}", messageType);
                break;
        }
    }

    private async Task DispatchSlaEventAsync(
        IServiceProvider serviceProvider,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var messageType = messageData.Type;
        logger.LogDebug("Received SLA event: {MessageType}", messageType);

        switch (messageType)
        {
            case nameof(DeadlinesCalculated):
                var deadlinesCalculated = serializer.DeserializeBinary<DeadlinesCalculated>(messageData.Payload);
                if (deadlinesCalculated is not null)
                {
                    var handler = serviceProvider.GetRequiredService<DeadlinesCalculatedHandler>();
                    await handler.HandleAsync(deadlinesCalculated, cancellationToken);
                }
                break;

            case nameof(SLABreached):
                var slaBreached = serializer.DeserializeBinary<SLABreached>(messageData.Payload);
                if (slaBreached is not null)
                {
                    var handler = serviceProvider.GetRequiredService<SLABreachedHandler>();
                    await handler.HandleAsync(slaBreached, cancellationToken);
                }
                break;

            default:
                logger.LogWarning("Unknown SLA event type: {MessageType}", messageType);
                break;
        }
    }

    private async Task DispatchAnonymizationEventAsync(
        IServiceProvider serviceProvider,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var messageType = messageData.Type;
        logger.LogDebug("Received anonymization event: {MessageType}", messageType);

        switch (messageType)
        {
            case nameof(AnonymizationRequested):
                var anonymizationRequested = serializer.DeserializeBinary<AnonymizationRequested>(messageData.Payload);
                if (anonymizationRequested is not null)
                {
                    var handler = serviceProvider.GetRequiredService<AnonymizationRequestedHandler>();
                    await handler.HandleAsync(anonymizationRequested, cancellationToken);
                }
                break;

            default:
                logger.LogWarning("Unknown anonymization event type: {MessageType}", messageType);
                break;
        }
    }
}

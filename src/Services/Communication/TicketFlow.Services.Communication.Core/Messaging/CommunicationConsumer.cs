using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Communication.Core.Messaging.Consuming;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Communication.Core.Messaging;

public class CommunicationConsumer(IMessageConsumer messageConsumer, AnomalySynchronizationConfigurator anomalyConfigurator) : BackgroundService
{
    public const string TicketResolvedQueue = "communication-ticket-resolved";
    public const string AlertsQueue = "communication-alerts";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer
            .ConsumeMessage<TicketResolved>(
                queue: TicketResolvedQueue,
                acceptedMessageTypes: null, /* Accept all of them */
                cancellationToken: stoppingToken);
        
        await messageConsumer
            .ConsumeMessage<ProducerAgnosticAlertMessage>(
                queue: AlertsQueue,
                acceptedMessageTypes: null, /* Accept all of them */
                cancellationToken: stoppingToken);
        
        await anomalyConfigurator.ConsumeAnomalyChanges();
    }
}
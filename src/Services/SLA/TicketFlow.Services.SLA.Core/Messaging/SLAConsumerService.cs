using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.CourseUtils;
using TicketFlow.Services.SLA.Core.Messaging.Consuming;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.Demultiplexing;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.Partitioning;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging;

public class SLAConsumerService(
    IMessageConsumer messageConsumer,
    AnomalySynchronizationConfigurator anomalyConfigurator,
    IServiceProvider serviceProvider,
    TicketChangesPartitioningSetup ticketChangesPartitioning) : BackgroundService
{
    public const string TicketChangesQueue = "sla-ticket-changes";
    public const string TicketQualifiedQueue = "sla-ticket-qualified";
    public const string AgentAssignedQueue = "sla-agent-assigned";
    public const string TicketResolvedQueue = "sla-ticket-resolved";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        #region topic-per-type
        if (FeatureFlags.UseTopicPerTypeExample)
        {
            await messageConsumer
                .ConsumeMessage<TicketQualified>(
                    queue: TicketQualifiedQueue,
                    cancellationToken: stoppingToken);

            await messageConsumer
                .ConsumeMessage<AgentAssignedToTicket>(
                    queue: AgentAssignedQueue,
                    cancellationToken: stoppingToken);
            
            await messageConsumer
                .ConsumeMessage<TicketResolved>(
                    queue: TicketResolvedQueue,
                    cancellationToken: stoppingToken);
        }
        #endregion
        #region topic-per-stream
        else if (FeatureFlags.UsePartitioningExample is false)
        {
            await messageConsumer
                .ConsumeNonGeneric(
                    handleRawPayload: async (messageData) =>
                    {
                        var demultiplexingHandler = CreateDemultiplexingHandler();
                        var logger = serviceProvider.GetService<ILogger<SLAConsumerService>>();
                        await demultiplexingHandler.HandleAsync(messageData, stoppingToken);
                    },
                    queue: TicketChangesQueue,
                    acceptedMessageTypes: ["TicketQualified", "AgentAssignedToTicket", "TicketBlocked", "TicketResolved"],
                    cancellationToken: stoppingToken);
        }
        #endregion
        #region topic-per-stream-with-partitioning
        else
        {
            await messageConsumer
                .ConsumeNonGenericFromPartitions(
                    ticketChangesPartitioning,
                    handleRawPayload: async (messageData) =>
                    {
                        var demultiplexingHandler = CreateDemultiplexingHandler();
                        var logger = serviceProvider.GetService<ILogger<SLAConsumerService>>();
                        await demultiplexingHandler.HandleAsync(messageData, stoppingToken);
                    },
                    queue: TicketChangesQueue,
                    acceptedMessageTypes: ["TicketQualified", "AgentAssignedToTicket", "TicketBlocked", "TicketResolved"],
                    cancellationToken: stoppingToken);
        }
        #endregion

        await anomalyConfigurator.ConsumeAnomalyChanges();
    }
    
    private TicketChangesHandler CreateDemultiplexingHandler()
    {
        var iocScope = serviceProvider.CreateScope();
        return iocScope.ServiceProvider.GetService<TicketChangesHandler>();
    }
}
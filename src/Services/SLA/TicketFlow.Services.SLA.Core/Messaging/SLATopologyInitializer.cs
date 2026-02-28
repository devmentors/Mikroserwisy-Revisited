using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Services.SLA.Core.Messaging.Consuming;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.Partitioning;
using TicketFlow.Services.SLA.Core.Messaging.Publishing.Conventions;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.SLA.Core.Messaging;

public class SLATopologyInitializer : TopologyInitializerBase
{
    public SLATopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider){}
    
    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        await CreateAlertingTopology(stoppingToken);
        await CreateAnomalySynchronizationTopology(stoppingToken);

        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: SLAMessagePublisherConventionProvider.ExchangeName,
            consumerDestination: "", // As publisher, we are consumer-ignorant
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken
        );

        if (FeatureFlags.UseTopicPerTypeExample)
        {
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: "tickets-exchange",
                consumerDestination: SLAConsumerService.TicketQualifiedQueue,
                TopologyType.PublishSubscribe,
                filter: "ticket-qualified",
                cancellationToken: stoppingToken
            );
            
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: "tickets-exchange",
                consumerDestination: SLAConsumerService.AgentAssignedQueue,
                TopologyType.PublishSubscribe,
                filter: "agent-assigned",
                cancellationToken: stoppingToken
            );
            
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: "tickets-exchange",
                consumerDestination: SLAConsumerService.TicketResolvedQueue,
                TopologyType.PublishSubscribe,
                filter: "ticket-resolved",
                cancellationToken: stoppingToken
            );
        }
        else if (FeatureFlags.UsePartitioningExample is false)
        {
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: "tickets-exchange",
                consumerDestination: SLAConsumerService.TicketChangesQueue,
                TopologyType.PublishSubscribe,
                cancellationToken: stoppingToken
            );
        }
        else
        {
            var ticketStatusPartitionOpts = ServiceProvider.GetService<TicketChangesPartitioningSetup>();
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: "tickets-exchange",
                consumerDestination: SLAConsumerService.TicketChangesQueue,
                TopologyType.PublishSubscribe,
                partitioningOptions: ticketStatusPartitionOpts.PartitioningOptions,
                cancellationToken: stoppingToken
            );
        }
    }

    private async Task CreateAlertingTopology(CancellationToken stoppingToken)
    {
        var alertingTopologyBuilder = new AlertingTopologyBuilder(
            ServiceProvider.GetService<ITopologyBuilder>(),
            ServiceProvider.GetService<IMessagePublisherConventionProvider>());

        await alertingTopologyBuilder.CreateTopologyAsync(stoppingToken);
    }
}
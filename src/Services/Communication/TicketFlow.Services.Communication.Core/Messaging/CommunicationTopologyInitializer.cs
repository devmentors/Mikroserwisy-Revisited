using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Communication.Core.Messaging;

public class CommunicationTopologyInitializer : TopologyInitializerBase
{
    public CommunicationTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {}

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        await CreateAnomalySynchronizationTopology(stoppingToken);
        
        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "tickets-exchange",
            consumerDestination: CommunicationConsumer.TicketResolvedQueue,
            TopologyType.PublishSubscribe,
            filter: "ticket-resolved",
            cancellationToken: stoppingToken
        );

        await topologyBuilder.CreateTopologyAsync(
            publisherSource: AlertingTopologyBuilder.AlertsExchange,
            consumerDestination: CommunicationConsumer.AlertsQueue,
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken
        );
    }
    
}
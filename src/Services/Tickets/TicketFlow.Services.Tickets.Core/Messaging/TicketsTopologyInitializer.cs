using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TicketCreated;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing.Conventions;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Tickets.Core.Messaging;

public class TicketsTopologyInitializer : TopologyInitializerBase
{
    public TicketsTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider){}

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        await CreateAlertingTopology(stoppingToken);
        await CreateAnomalySynchronizationTopology(stoppingToken);

        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
            consumerDestination: "", // As publisher, we are consumer-ignorant
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken
        );

        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "sla-exchange",
            consumerDestination: TicketsConsumerService.SLAChangesQueue,
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        // Topic-per-type: InquirySubmitted (exchange per message type)
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "inquiry-submitted-exchange",
            consumerDestination: TicketsConsumerService.InquirySubmittedQueue,
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        // Topic-per-type: TranslationCompleted (exchange per message type)
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "translation-completed-exchange",
            consumerDestination: TicketsConsumerService.TranslationCompletedQueue,
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        if (FeatureFlags.UseListenToYourselfExample)
        {
            await topologyBuilder.CreateTopologyAsync(
                publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
                consumerDestination: TicketsConsumerService.TicketCreatedQueue,
                TopologyType.PublishSubscribe,
                cancellationToken: stoppingToken);
        }

        // Bind to anonymization-exchange for AnonymizationRequested events
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "anonymization-exchange",
            consumerDestination: TicketsConsumerService.AnonymizationQueue,
            TopologyType.PublishSubscribe,
            filter: "anonymization-requested",
            cancellationToken: stoppingToken);

        // Exchange-to-exchange: forward AnonymizationCompleted to anonymization-exchange
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
            consumerDestination: "anonymization-exchange",
            TopologyType.PublisherToPublisher,
            filter: "*.anonymization.#",
            cancellationToken: stoppingToken);

        // Rebalance queue - listen to own TicketStatusChanged events
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: TicketsMessagePublisherConventionProvider.ExchangeName,
            consumerDestination: TicketsConsumerService.RebalanceQueue,
            TopologyType.PublishSubscribe,
            filter: "ticket-status-changed",
            cancellationToken: stoppingToken);
    }
    
    private async Task CreateAlertingTopology(CancellationToken stoppingToken)
    {
        var alertingTopologyBuilder = new AlertingTopologyBuilder(
            ServiceProvider.GetService<ITopologyBuilder>(),
            ServiceProvider.GetService<IMessagePublisherConventionProvider>());

        await alertingTopologyBuilder.CreateTopologyAsync(stoppingToken);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Aggregation.Core.Messaging;

public class AggregationTopologyInitializer : TopologyInitializerBase
{
    private const string TicketEventsQueue = "aggregation-ticket-events";
    private const string SlaEventsQueue = "aggregation-sla-events";
    public const string AggregationExchange = "aggregation-exchange";
    private readonly ILogger<AggregationTopologyInitializer> _logger;

    public AggregationTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AggregationTopologyInitializer>>();
    }

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        var topologyBuilder = ServiceProvider.GetRequiredService<ITopologyBuilder>();

        // Create aggregation-exchange for publishing
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: AggregationExchange,
            consumerDestination: "",
            topologyType: TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        // Bind all ticket events with # (topic exchange)
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "tickets-exchange",
            consumerDestination: TicketEventsQueue,
            topologyType: TopologyType.PublishSubscribe,
            filter: "#",
            cancellationToken: stoppingToken);

        // Bind SLA events
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "sla-exchange",
            consumerDestination: SlaEventsQueue,
            topologyType: TopologyType.PublishSubscribe,
            filter: "#",
            cancellationToken: stoppingToken);

        // Bind queue to anonymization-exchange for AnonymizationRequested
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "anonymization-exchange",
            consumerDestination: AggregationConsumerService.AnonymizationQueue,
            topologyType: TopologyType.PublishSubscribe,
            filter: "anonymization-requested",
            cancellationToken: stoppingToken);

        // Exchange-to-exchange: forward AnonymizationCompleted to anonymization-exchange
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: AggregationExchange,
            consumerDestination: "anonymization-exchange",
            topologyType: TopologyType.PublisherToPublisher,
            filter: "*.anonymization.#",
            cancellationToken: stoppingToken);

        _logger.LogInformation("Topology initialized - queues bound to exchanges");
    }
}

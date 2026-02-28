using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Anonymization.Core.Messaging;

public class AnonymizationTopologyInitializer : TopologyInitializerBase
{
    private const string AnonymizationEventsQueue = "anonymization-events-queue";
    private readonly ILogger<AnonymizationTopologyInitializer> _logger;

    public AnonymizationTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AnonymizationTopologyInitializer>>();
    }

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        var topologyBuilder = ServiceProvider.GetRequiredService<ITopologyBuilder>();

        // Create exchange for publishing AnonymizationRequested events
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "anonymization-exchange",
            consumerDestination: "",
            topologyType: TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        // Bind queue for consuming AnonymizationCompleted events
        // Pattern *.anonymization.# matches routing keys like "tickets.anonymization.completed"
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "anonymization-exchange",
            consumerDestination: AnonymizationEventsQueue,
            topologyType: TopologyType.PublishSubscribe,
            filter: "*.anonymization.#",
            cancellationToken: stoppingToken);

        _logger.LogInformation("Topology initialized - exchange created and queue bound");
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.PersonalInfoVault.Core.Messaging;

public class VaultTopologyInitializer : TopologyInitializerBase
{
    public const string VaultExchange = "vault-exchange";
    public const string AnonymizationQueue = "vault-anonymization-queue";
    private readonly ILogger<VaultTopologyInitializer> _logger;

    public VaultTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<VaultTopologyInitializer>>();
    }

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        var topologyBuilder = ServiceProvider.GetRequiredService<ITopologyBuilder>();

        // Create vault-exchange for publishing
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: VaultExchange,
            consumerDestination: "",
            topologyType: TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken);

        // Bind to anonymization-exchange for AnonymizationRequested events
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "anonymization-exchange",
            consumerDestination: AnonymizationQueue,
            topologyType: TopologyType.PublishSubscribe,
            filter: "anonymization-requested",
            cancellationToken: stoppingToken);

        // Exchange-to-exchange: forward AnonymizationCompleted to anonymization-exchange
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: VaultExchange,
            consumerDestination: "anonymization-exchange",
            topologyType: TopologyType.PublisherToPublisher,
            filter: "*.anonymization.#",
            cancellationToken: stoppingToken);

        _logger.LogInformation("Topology initialized - queue bound to anonymization-exchange");
    }
}

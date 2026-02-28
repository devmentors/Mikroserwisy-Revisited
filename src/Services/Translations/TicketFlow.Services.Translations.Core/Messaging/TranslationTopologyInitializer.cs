using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Translations.Core.Messaging;

public class TranslationTopologyInitializer : TopologyInitializerBase
{
    public TranslationTopologyInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async Task CreateTopologyAsync(CancellationToken stoppingToken)
    {
        var topologyBuilder = ServiceProvider.GetService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "",
            consumerDestination: "request-translation-v1-queue",
            TopologyType.Direct,
            cancellationToken: stoppingToken
        );
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: "",
            consumerDestination: "request-translation-v2-queue",
            TopologyType.Direct,
            cancellationToken: stoppingToken
        );
    }
}
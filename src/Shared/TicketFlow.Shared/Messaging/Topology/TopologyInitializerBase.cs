using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.App;

namespace TicketFlow.Shared.Messaging.Topology;

public abstract class TopologyInitializerBase : BackgroundService
{
    protected readonly IServiceProvider ServiceProvider;
    private readonly TopologyReadinessAccessor _topologyReadinessAccessor;
    private readonly TopologyOptions _topologyOptions;
    

    public TopologyInitializerBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        
        _topologyReadinessAccessor = ServiceProvider.GetService<TopologyReadinessAccessor>();
        _topologyReadinessAccessor.MarkTopologyProvisioningStart(GetType().Name);
        
        _topologyOptions = ServiceProvider.GetService<TopologyOptions>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_topologyOptions.CreateTopology)
        {
            await CreateTopologyAsync(stoppingToken);
        }
        else
        {
            await CreateAnomalySynchronizationTopology(stoppingToken);
        }
        
        _topologyReadinessAccessor.MarkTopologyProvisioningEnd(GetType().Name);
        
    }
    
    protected abstract Task CreateTopologyAsync(CancellationToken stoppingToken);
    
    protected async Task CreateAnomalySynchronizationTopology(CancellationToken stoppingToken)
    {
        var anomalyTopologyBuilder = new AnomalyTopologyBuilder(
            ServiceProvider.GetService<ITopologyBuilder>(),
            ServiceProvider.GetService<IOptions<AppOptions>>());

        await anomalyTopologyBuilder.CreateTopologyAsync(stoppingToken);
    }
}
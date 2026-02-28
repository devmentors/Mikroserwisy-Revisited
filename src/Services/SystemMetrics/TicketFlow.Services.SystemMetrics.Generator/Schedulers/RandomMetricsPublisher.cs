using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketFlow.Services.SystemMetrics.Generator.Data;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.SystemMetrics.Generator.Schedulers;

public class RandomMetricsPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AppOptions> _appOptions;
    private readonly TopologyReadinessAccessor _topologyReadinessAccessor;
    
    public RandomMetricsPublisher(IServiceProvider serviceProvider, IOptions<AppOptions> appOptions)
    {
        _serviceProvider = serviceProvider;
        _appOptions = appOptions;
        _topologyReadinessAccessor = _serviceProvider.GetService<TopologyReadinessAccessor>();
        _topologyReadinessAccessor.MarkTopologyProvisioningStart(GetType().Name);
        
    }

    public const string MetricsExchange = "metrics-exchange";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var iocScope = _serviceProvider.CreateScope();
        var publishers = iocScope.ServiceProvider.GetRequiredService<IEnumerable<IMessagePublisher>>();
        // Unfortunately - we need to be "outbox" aware here to not push transient metric messages through outbox (and "pollute it")
        var publisher = publishers.FirstOrDefault(x => !x.GetType().Name.Contains("Outbox"));
        
        var topologyBuilder = iocScope.ServiceProvider.GetRequiredService<ITopologyBuilder>();
        
        await topologyBuilder.CreateTopologyAsync(
            publisherSource: MetricsExchange,
            consumerDestination: "", // As publisher, we are consumer-ignorant
            TopologyType.PublishSubscribe,
            cancellationToken: stoppingToken
        );
        
        _topologyReadinessAccessor.MarkTopologyProvisioningEnd(GetType().Name);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var tick = MetricsGenerator.GenerateTickForService(_appOptions.Value.AppName);
            if (tick != null)
            {
                await publisher.PublishAsync(
                    tick, 
                    destination: MetricsExchange, 
                    routingKey: "metric." + _appOptions.Value.AppName, 
                    cancellationToken: stoppingToken);
            }

            await Task.Delay(2000, cancellationToken: stoppingToken);
        }
    }
}
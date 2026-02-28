using Microsoft.AspNetCore.SignalR;
using TicketFlow.Services.SystemMetrics.Generator.Data;
using TicketFlow.Services.SystemMetrics.Generator.Schedulers;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.SystemMetrics.Core.LiveMetrics;

public class LiveMetricsHub(IMessageConsumer messageConsumer, ITopologyBuilder topologyBuilder) : Hub
{
    public const string LiveMetricsQueue = "live-metrics";
    
    private object _connectLock = new object();
    private int _connectedUsersCount = 0;
    
    // ReSharper disable once InconsistentlySynchronizedField
    public bool HasActiveClients => _connectedUsersCount > 0;

    public override Task OnConnectedAsync()
    {
        lock (_connectLock)
        {
            if (_connectedUsersCount == 0)
            {
                EnsureMetricsQueue();
            }

            _connectedUsersCount++;
        }
        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_connectLock)
        {
            _connectedUsersCount--;
        }
        
        return base.OnDisconnectedAsync(exception);
    }

    public async Task PushOldestMetricTick(MetricTick? metricTick)
    {
        if (metricTick == null)
        {
            return;
        }
        await Clients.All.SendAsync("MetricTick", metricTick);
    }

    private void EnsureMetricsQueue()
    {
        topologyBuilder.CreateTopologyAsync(
            publisherSource: RandomMetricsPublisher.MetricsExchange,
            consumerDestination: LiveMetricsQueue,
            TopologyType.PublishSubscribe,
            consumerCustomArgs: new Dictionary<string, object>
            {
                { "x-expires", 1000 * 60 * 1 } // Delete queue if no one is listening after 1min
            }
        ).GetAwaiter().GetResult();
    }
}
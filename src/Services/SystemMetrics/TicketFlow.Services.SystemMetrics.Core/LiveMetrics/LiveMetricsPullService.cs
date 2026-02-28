using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.SystemMetrics.Generator.Data;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SystemMetrics.Core.LiveMetrics;

public class LiveMetricsPullService(IMessageConsumer messageConsumer, LiveMetricsHub hub, LiveMetricsOptions opts) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!hub.HasActiveClients)
                {
                    await Task.Delay(opts.PollingIntervalInMs, stoppingToken);
                    continue;
                }

                await messageConsumer.GetMessage<MetricTick>(
                    handle: async msg => await hub.PushOldestMetricTick(msg),
                    LiveMetricsHub.LiveMetricsQueue,
                    stoppingToken);

                await Task.Delay(opts.PollingIntervalInMs, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while pulling the message from broker: {0}", ex.Message);
            }
        }
    }
}
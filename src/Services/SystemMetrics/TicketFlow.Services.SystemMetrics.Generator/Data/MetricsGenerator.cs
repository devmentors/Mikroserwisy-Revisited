using System.Collections.Immutable;

namespace TicketFlow.Services.SystemMetrics.Generator.Data;

public static class MetricsGenerator
{
    private static readonly Random Random = new();

    private const double ServiceDownProbability = 0.2;
    private const int MinRamUsage = 128;
    private const int MaxAvailableRam = 2048;
    private const int MinCpuUsage = 0;
    private const int MaxCpuUsage = 100;
    private const int UnhealthyCpuThreshold = 90;
    private static readonly int UnhealthyRamThreshold = (int)Math.Floor(MaxAvailableRam * 0.9);

    public static MetricTick? GenerateTickForService(string serviceName)
    {
        var utcNow = DateTimeOffset.UtcNow;
        
        var serviceDown = Random.NextDouble() <= ServiceDownProbability;
        if (serviceDown)
        {
            return default;
        }
            
        var cpuValue = Random.Next(MinCpuUsage, MaxCpuUsage + 1);
        var ramValue = Random.Next(MinRamUsage, MaxAvailableRam + 1);
            
        var isHealthy = cpuValue < UnhealthyCpuThreshold && ramValue < UnhealthyRamThreshold;
        var status = isHealthy ? MetricStatus.Healthy : MetricStatus.Error;

        return new MetricTick(
            serviceName,
            status,
            utcNow,
            new Metrics(
                new CpuUsage(cpuValue),
                new MemoryUsage(ramValue, MaxAvailableRam)
            )
        );
    }
}
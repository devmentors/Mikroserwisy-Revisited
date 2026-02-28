using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Resiliency;

namespace TicketFlow.Services.SystemMetrics.Generator.Data;

public record MetricTick(
    string Name,
    MetricStatus Status,
    DateTimeOffset LastChecked,
    Metrics Metrics) : INonMandatoryMessage;

public record Metrics(CpuUsage Cpu, MemoryUsage Memory);
public record CpuUsage(int Usage);
public record MemoryUsage(int Used, int Total);

public enum MetricStatus
{
    Unknown,
    Healthy,
    Error
}
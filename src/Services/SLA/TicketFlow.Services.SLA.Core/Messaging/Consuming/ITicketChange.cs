using TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming;

public interface ITicketChange : IVersionedMessage
{
    Guid TicketId { get; }
}

public static class TicketChangeExtensions
{
    public static string ToHumanReadableChange(this ITicketChange change) => 
        $"[{change.GetType().Name}] ID: {change.TicketId} || VERSION: {change.Version}";
}
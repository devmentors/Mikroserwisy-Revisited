namespace TicketFlow.Services.SLA.Core.Messaging.Consuming.Demultiplexing;

public record FallbackTicketChangeEvent(Guid TicketId, int Version) : ITicketChange
{
    public string ToHumanReadableString() => this.ToHumanReadableChange();
}
namespace TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

public interface IVersionedMessage : IMessage
{
    int Version { get; }
    string ToHumanReadableString();
}
namespace TicketFlow.Shared.Messaging.Partitioning;

public interface IMessageWithPartitionKey
{
    string PartitionKey { get; }
}
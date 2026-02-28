namespace TicketFlow.Shared.Messaging.Partitioning;

public interface IConsumerSpecificPartitioningSetup
{
    List<int> PartitionNumbersToConsume { get; }
    PartitioningOptions PartitioningOptions { get; }
}
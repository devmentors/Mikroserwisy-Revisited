namespace TicketFlow.Shared.Messaging.Partitioning;

public record PartitioningOptions(int NumberOfPartitions, bool OnlyOneActiveConsumerPerPartition)
{
    public static PartitioningOptions Default => new(NumberOfPartitions: 1, OnlyOneActiveConsumerPerPartition: false);
}
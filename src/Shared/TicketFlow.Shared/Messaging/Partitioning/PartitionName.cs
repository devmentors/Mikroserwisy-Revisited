namespace TicketFlow.Shared.Messaging.Partitioning;

public static class PartitionName
{
    public static string ForConsumerDedicatedExchange(string targetQueueName) => $"{targetQueueName}-partitioned-exchange";
    public static string ForQueue(string queueName, int partitionNum) => $"{queueName}-partition-{partitionNum}";
}
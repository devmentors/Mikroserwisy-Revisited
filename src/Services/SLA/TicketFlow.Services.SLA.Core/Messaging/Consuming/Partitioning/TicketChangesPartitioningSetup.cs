using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming.Partitioning;

public record TicketChangesPartitioningSetup(
    List<int> PartitionNumbersToConsume,
    PartitioningOptions PartitioningOptions) : IConsumerSpecificPartitioningSetup;
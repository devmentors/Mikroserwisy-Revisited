using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Shared.Messaging.Topology;

public interface ITopologyBuilder
{
    Task CreateTopologyAsync(
        string publisherSource,
        string consumerDestination,
        TopologyType topologyType,
        string filter = "",
        Dictionary<string, object>? consumerCustomArgs = default,
        PartitioningOptions? partitioningOptions = default,
        CancellationToken cancellationToken = default);
}

public enum TopologyType
{
    Direct,
    PublishSubscribe,
    PublisherToPublisher
}
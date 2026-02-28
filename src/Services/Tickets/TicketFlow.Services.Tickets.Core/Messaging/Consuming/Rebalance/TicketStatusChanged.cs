using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.Rebalance;

/// <summary>
/// Event published when a ticket's status changes.
/// Used to trigger rebalancing when tickets are resolved (capacity freed up).
/// </summary>
public record TicketStatusChanged(
    Guid TicketId,
    string? OldStatus,
    string? NewStatus,
    Guid? AgentId) : IMessage, IMessageWithPartitionKey
{
    public string PartitionKey => TicketId.ToString();
}

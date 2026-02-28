using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.Communication.Core.Messaging;

public record TicketResolved(Guid TicketId, int Version) : IMessage, IMessageWithPartitionKey
{
    public string PartitionKey => TicketId.ToString();
}
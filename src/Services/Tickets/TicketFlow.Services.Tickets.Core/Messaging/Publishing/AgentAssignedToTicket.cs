using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.Tickets.Core.Messaging.Publishing;

public record AgentAssignedToTicket(Guid TicketId, int Version) : IMessage, IMessageWithPartitionKey
{
    public string PartitionKey => TicketId.ToString();
}
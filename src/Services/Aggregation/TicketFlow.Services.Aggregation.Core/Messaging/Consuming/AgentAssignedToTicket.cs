using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record AgentAssignedToTicket(Guid TicketId, int Version) : IMessage;

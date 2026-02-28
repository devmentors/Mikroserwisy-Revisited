using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record TicketResolved(Guid TicketId, int Version) : IMessage;

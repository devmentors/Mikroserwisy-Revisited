using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record TicketQualified(Guid TicketId, int Version) : IMessage;

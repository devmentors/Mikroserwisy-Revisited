using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record AnonymizationRequested(
    Guid RequestId,
    string PersonToken,
    DateTimeOffset RequestedAt) : IMessage;

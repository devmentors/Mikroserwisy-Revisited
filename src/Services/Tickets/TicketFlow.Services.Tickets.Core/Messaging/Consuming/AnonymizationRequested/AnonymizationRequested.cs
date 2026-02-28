using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.AnonymizationRequested;

public record AnonymizationRequested(
    Guid RequestId,
    string PersonToken,
    DateTimeOffset RequestedAt) : IMessage;

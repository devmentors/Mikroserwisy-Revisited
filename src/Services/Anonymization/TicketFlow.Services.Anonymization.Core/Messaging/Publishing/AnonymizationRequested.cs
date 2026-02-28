using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Anonymization.Core.Messaging.Publishing;

public record AnonymizationRequested(
    Guid RequestId,
    string PersonToken,
    DateTimeOffset RequestedAt) : IMessage;

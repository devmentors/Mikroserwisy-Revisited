using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Publishing;

public record AnonymizationCompleted(
    Guid RequestId,
    string ServiceName,
    bool Success,
    string? ErrorMessage) : IMessage;

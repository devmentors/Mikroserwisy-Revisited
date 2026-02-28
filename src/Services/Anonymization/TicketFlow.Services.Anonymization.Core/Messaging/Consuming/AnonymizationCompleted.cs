using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Anonymization.Core.Messaging.Consuming;

public record AnonymizationCompleted(
    Guid RequestId,
    string ServiceName,
    bool Success,
    string? ErrorMessage) : IMessage;

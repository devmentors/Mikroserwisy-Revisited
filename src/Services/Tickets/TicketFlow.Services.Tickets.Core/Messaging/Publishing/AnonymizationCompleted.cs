using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Publishing;

public record AnonymizationCompleted(
    Guid RequestId,
    string ServiceName,
    bool Success,
    string? ErrorMessage) : IMessage;

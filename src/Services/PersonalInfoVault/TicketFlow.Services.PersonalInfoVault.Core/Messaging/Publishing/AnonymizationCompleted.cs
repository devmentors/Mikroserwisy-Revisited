using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.PersonalInfoVault.Core.Messaging.Publishing;

public record AnonymizationCompleted(
    Guid RequestId,
    string ServiceName,
    bool Success,
    string? ErrorMessage) : IMessage;

using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.PersonalInfoVault.Core.Messaging.Consuming;

public record AnonymizationRequested(
    Guid RequestId,
    string PersonToken,
    DateTimeOffset RequestedAt) : IMessage;

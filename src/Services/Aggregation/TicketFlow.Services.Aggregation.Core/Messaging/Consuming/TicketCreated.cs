using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record TicketCreated(
    Guid Id,
    Guid InquiryId,
    string PersonToken,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    int Version = 1) : IMessage;

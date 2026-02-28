using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.Contracts.Inquiries.Events;

public sealed record InquirySubmitted(
    Guid Id,
    Guid? UserId,
    string PersonToken,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    DateTimeOffset CreatedAt) : IMessage;

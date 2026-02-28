using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Publishing;

public sealed record InquirySubmitted(
    Guid Id,
    string PersonToken,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    DateTimeOffset CreatedAt) : IMessage;
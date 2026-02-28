using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.Contracts.Inquiries.Events;

public sealed record InquirySubmitted(
    Guid Id,
    string PersonToken,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    DateTimeOffset CreatedAt,
    string? SubmittedVia,
    string? ClientIpHash) : IMessage;

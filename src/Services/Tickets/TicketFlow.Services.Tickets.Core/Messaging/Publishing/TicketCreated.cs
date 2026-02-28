using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.Tickets.Core.Messaging.Publishing;

public record TicketCreated(
    Guid Id,
    Guid InquiryId,
    string PersonToken,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    int Version = 1) : IMessage, IMessageWithPartitionKey
{
    public string PartitionKey => Id.ToString();
}
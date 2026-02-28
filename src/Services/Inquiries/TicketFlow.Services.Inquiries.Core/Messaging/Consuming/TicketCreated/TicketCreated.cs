using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Consuming.TicketCreated;

public record TicketCreated(Guid Id, Guid InquiryId, int Version) : IMessage;
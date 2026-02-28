using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Publishing;

public sealed record RequestTranslationV1(string Text, Guid TicketId) : IMessage;
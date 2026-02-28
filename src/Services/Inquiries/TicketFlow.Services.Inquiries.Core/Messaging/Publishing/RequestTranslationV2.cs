using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Publishing;

public sealed record RequestTranslationV2(string Text, string LanguageCode, Guid ReferenceId) : IMessage;
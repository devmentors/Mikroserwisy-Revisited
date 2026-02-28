using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

public sealed record RequestTranslationV1(string Text, Guid TicketId) : IMessage;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

public sealed record RequestTranslationV2(string Text, string LanguageCode, Guid ReferenceId) : IMessage;
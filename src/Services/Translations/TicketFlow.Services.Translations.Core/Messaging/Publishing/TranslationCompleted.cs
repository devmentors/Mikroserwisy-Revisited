using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Publishing;

public record TranslationCompleted(string OriginalText, string TranslatedText, Guid ReferenceId) : IMessage;
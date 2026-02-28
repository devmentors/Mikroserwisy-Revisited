using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Publishing;

public record TranslationSkipped(string OriginalText, Guid ReferenceId) : IMessage;
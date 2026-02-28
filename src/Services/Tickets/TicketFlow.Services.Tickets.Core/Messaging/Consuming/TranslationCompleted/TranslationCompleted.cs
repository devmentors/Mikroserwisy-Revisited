using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;

public record TranslationCompleted(string OriginalText, string TranslatedText, Guid ReferenceId) : IMessage;
using TicketFlow.Services.Translations.Core.Messaging.Publishing;
using TicketFlow.Services.Translations.Core.Translations;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;

internal sealed class RequestTranslationHandler(ITranslationsService translationsService, IMessagePublisher messagePublisher) 
    : IMessageHandler<RequestTranslationV1>, IMessageHandler<RequestTranslationV2>
{
    public Task HandleAsync(RequestTranslationV1 message, CancellationToken cancellationToken = default)
        => HandleAsync(message.Text, default, TranslationLanguage.English, message.TicketId, cancellationToken);

    public Task HandleAsync(RequestTranslationV2 message, CancellationToken cancellationToken = default)
        => HandleAsync(message.Text, message.LanguageCode, TranslationLanguage.English, message.ReferenceId, cancellationToken);
    
    private async Task HandleAsync(string text, string translateFrom, string languageCode, Guid referenceId, CancellationToken cancellationToken = default)
    {
        var translatedText = await translationsService.TranslateAsync(text, translateFrom, languageCode, cancellationToken);

        if (string.IsNullOrWhiteSpace(translatedText))
        {
            var translationSkippedMessage = new TranslationSkipped(text, referenceId);
            await messagePublisher.PublishAsync(translationSkippedMessage, destination: "translation-completed-exchange", cancellationToken: cancellationToken);
            return;
        }
        
        var translationCompletedMessage = new TranslationCompleted(text, translatedText, referenceId);
        await messagePublisher.PublishAsync(translationCompletedMessage, destination: "translation-completed-exchange", cancellationToken: cancellationToken);
    }
}
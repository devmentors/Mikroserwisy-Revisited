using Microsoft.Extensions.Hosting;
using TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Translations.Core.Messaging;

internal sealed class TranslationsConsumerService(IMessageConsumer messageConsumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        //messageConsumer.ConsumeMessage<RequestTranslationV1>(queue: "request-translation-v1-queue", cancellationToken: cancellationToken);
        await messageConsumer.ConsumeMessage<RequestTranslationV2>(queue: "request-translation-v2-queue", cancellationToken: cancellationToken);
    }
}
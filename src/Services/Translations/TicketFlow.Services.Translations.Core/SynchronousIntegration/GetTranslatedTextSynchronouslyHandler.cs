using TicketFlow.Services.Translations.Core.Translations;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Translations.Core.SynchronousIntegration;

internal sealed class GetTranslatedTextSynchronouslyHandler(ITranslationsService translationsService) : IQueryHandler<GetTranslatedTextSynchronously, string>
{
    public async Task<string> HandleAsync(GetTranslatedTextSynchronously query, CancellationToken cancellationToken = default)
    {
        var translatedText = await translationsService.TranslateAsync(query.Text, default, "en", cancellationToken);
        return translatedText;
    }
}
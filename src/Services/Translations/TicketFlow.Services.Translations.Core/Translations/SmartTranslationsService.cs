namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class SmartTranslationsService(
    ElevenLabsTranslationsService elevenLabsService,
    OpenAiTranslationsService openAiService) : ITranslationsService
{
    private static readonly string[] ElevenLabsLanguages = ["pl", "de", "es"];

    public Task<string> TranslateAsync(string text, string? translateFrom, string translateTo,
        CancellationToken cancellationToken = default)
    {
        if (ShouldUseElevenLabs(translateFrom))
        {
            return elevenLabsService.TranslateAsync(text, translateFrom, translateTo, cancellationToken);
        }

        return openAiService.TranslateAsync(text, translateFrom, translateTo, cancellationToken);
    }

    private static bool ShouldUseElevenLabs(string? languageCode)
        => languageCode is not null && ElevenLabsLanguages.Contains(languageCode, StringComparer.OrdinalIgnoreCase);
}

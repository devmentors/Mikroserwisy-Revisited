namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class ElevenLabsTranslationsService : ITranslationsService
{
    public Task<string> TranslateAsync(string text, string? translateFrom, string translateTo,
        CancellationToken cancellationToken = default)
    {
        var result = $"[ElevenLabs: {translateFrom} → {translateTo}] {text}";
        return Task.FromResult(result);
    }
}

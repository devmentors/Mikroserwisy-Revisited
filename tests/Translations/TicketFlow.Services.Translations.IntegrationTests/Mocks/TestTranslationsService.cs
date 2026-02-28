using TicketFlow.Services.Translations.Core.Translations;

namespace TicketFlow.Services.Translations.IntegrationTests.Mocks;

public class TestTranslationsService : ITranslationsService
{
    public string TranslatedText { get; set; }
    
    public Task<string> TranslateAsync(string text, string? translateFrom, string translateTo, CancellationToken cancellationToken = default)
        => Task.FromResult(TranslatedText);
}
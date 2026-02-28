namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class NoopTranslationsService : ITranslationsService
{
    public Task<string> TranslateAsync(string text, string translateFrom, string translateTo, CancellationToken cancellationToken = default)
        => Task.FromResult("This is Noop translation");
}
using Microsoft.Extensions.AI;

namespace TicketFlow.Services.Translations.Core.Translations;

internal sealed class OpenAiTranslationsService(IChatClient chatClient) : ITranslationsService
{
    public async Task<string> TranslateAsync(string text, string? translateFrom, string translateTo, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Translate {(translateFrom is null ? "" : $"from {translateFrom}")} to {translateTo}: '{text}'";
        var response = await chatClient.CompleteAsync(prompt, cancellationToken: cancellationToken);

        return response.Message.Text!;
    }
}
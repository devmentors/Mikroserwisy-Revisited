using Microsoft.Extensions.AI;

namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

internal sealed class OpenAiLanguageDetector(IChatClient chatClient) : ILanguageDetector
{
    public async Task<string> GetTextLanguageCode(string text, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Respond only with language code that you detect. Text:'{text}'";
        var response = await chatClient.CompleteAsync(prompt, cancellationToken: cancellationToken);

        return response.Message.Text!;
    }
}
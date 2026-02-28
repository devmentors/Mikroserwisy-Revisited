using TicketFlow.Services.Inquiries.Core.LanguageDetection;

namespace TicketFlow.Services.Inquiries.IntegrationTests.Mocks;

internal sealed class TestLanguageDetector : ILanguageDetector
{
    public string ReturnedLanguage { get; set; }
    
    public Task<string> GetTextLanguageCode(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(ReturnedLanguage ?? "en");
}
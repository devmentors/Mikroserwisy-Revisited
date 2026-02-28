namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

internal sealed class NoopLanguageDetector : ILanguageDetector
{
    public Task<string> GetTextLanguageCode(string text, CancellationToken cancellationToken = default)
        => Task.FromResult("en");
}
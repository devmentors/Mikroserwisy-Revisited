using TicketFlow.Services.Inquiries.Core.LanguageDetection;

namespace TicketFlow.Services.Inquiries.IntegrationTests.Mocks;

internal sealed class TestLanguageDetector : ILanguageDetector
{
    public string? ReturnedLanguage { get; set; }

    public LanguageDetectionOutcome Outcome { get; set; } = LanguageDetectionOutcome.Detected;

    public Task<LanguageDetectionResult> DetectAsync(string text, CancellationToken cancellationToken = default)
    {
        if (Outcome is not LanguageDetectionOutcome.Detected)
        {
            return Task.FromResult(Outcome is LanguageDetectionOutcome.Failed
                ? LanguageDetectionResult.Failed
                : LanguageDetectionResult.Unrecognized);
        }

        return Task.FromResult(LanguageCode.TryParse(ReturnedLanguage ?? "en", out var code)
            ? LanguageDetectionResult.Detected(code)
            : LanguageDetectionResult.Unrecognized);
    }
}

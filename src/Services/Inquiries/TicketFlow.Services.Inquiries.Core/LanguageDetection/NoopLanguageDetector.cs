namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

internal sealed class NoopLanguageDetector : ILanguageDetector
{
    private static readonly LanguageDetectionResult Polish =
        LanguageCode.TryParse("pl", out var code)
            ? LanguageDetectionResult.Detected(code)
            : LanguageDetectionResult.Unrecognized;

    public Task<LanguageDetectionResult> DetectAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(Polish);
}

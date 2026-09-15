namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

public interface ILanguageDetector
{
    /// <summary>
    /// Detects the language of <paramref name="text"/>. Never throws: a failed or
    /// unusable detection is returned as a result, because language detection is not
    /// a good enough reason to fail an inquiry that has already been accepted.
    /// </summary>
    Task<LanguageDetectionResult> DetectAsync(string text, CancellationToken cancellationToken = default);
}

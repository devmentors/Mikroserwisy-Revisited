namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

/// <summary>
/// The single place that decides whether an inquiry needs translating. Two handlers
/// used to answer this with their own copy of <c>languageCode is not "en"</c>, which is
/// how they ended up disagreeing about what an unknown language means.
/// </summary>
public static class TranslationPolicy
{
    /// <summary>
    /// Translate only when a language was actually detected and it is not English.
    /// "Could not recognise" and "the call failed" deliberately do not trigger a
    /// translation: paying a model to translate text nobody could classify is exactly
    /// how English inquiries ended up in the translation queue.
    /// </summary>
    public static bool ShouldTranslate(LanguageDetectionResult detection)
        => detection.Outcome is LanguageDetectionOutcome.Detected
           && detection.Code is { IsEnglish: false };
}

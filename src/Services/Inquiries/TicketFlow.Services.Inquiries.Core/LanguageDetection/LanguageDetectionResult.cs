namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

/// <summary>
/// Why a detection ended the way it did. "I could not tell" and "the call blew up" are
/// deliberately separate from "this text is English" — they look the same to a caller that
/// only compares strings, and that is exactly the bug this type exists to prevent.
/// </summary>
public enum LanguageDetectionOutcome
{
    Detected,
    Unrecognized,
    Failed
}

public sealed record LanguageDetectionResult(LanguageCode? Code, LanguageDetectionOutcome Outcome)
{
    public static readonly LanguageDetectionResult Unrecognized = new(null, LanguageDetectionOutcome.Unrecognized);

    public static readonly LanguageDetectionResult Failed = new(null, LanguageDetectionOutcome.Failed);

    public static LanguageDetectionResult Detected(LanguageCode code) => new(code, LanguageDetectionOutcome.Detected);

    /// <summary>Code to persist and publish. Undetermined when nothing could be established.</summary>
    public string CodeOrUndetermined => Code?.Value ?? LanguageCode.UndeterminedValue;
}

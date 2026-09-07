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

/// <summary>
/// The outcome and the code are paired at construction and cannot be recombined: there is no
/// public constructor and no <c>with</c> expression, so a Detected result always carries a code
/// and an Unrecognized or Failed one never does.
/// </summary>
public sealed record LanguageDetectionResult
{
    private LanguageDetectionResult(LanguageCode? code, LanguageDetectionOutcome outcome)
    {
        Code = code;
        Outcome = outcome;
    }

    public LanguageCode? Code { get; }

    public LanguageDetectionOutcome Outcome { get; }

    public static readonly LanguageDetectionResult Unrecognized =
        new(null, LanguageDetectionOutcome.Unrecognized);

    public static readonly LanguageDetectionResult Failed =
        new(null, LanguageDetectionOutcome.Failed);

    public static LanguageDetectionResult Detected(LanguageCode code)
        => new(code, LanguageDetectionOutcome.Detected);

    /// <summary>Code to persist and publish. Undetermined when nothing could be established.</summary>
    public string CodeOrUndetermined => Code?.Value ?? LanguageCode.UndeterminedValue;
}

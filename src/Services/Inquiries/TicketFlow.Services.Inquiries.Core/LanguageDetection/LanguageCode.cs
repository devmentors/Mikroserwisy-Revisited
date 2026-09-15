using System.Globalization;

namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

/// <summary>
/// An ISO 639-1 language code. Instances can only be produced through <see cref="TryParse"/>,
/// so anything that holds a <see cref="LanguageCode"/> holds a value the rest of the system
/// can safely compare against — as opposed to whatever free-form text a model happened to return.
/// </summary>
public readonly record struct LanguageCode
{
    public const string EnglishValue = "en";

    /// <summary>ISO 639-2 code reserved for "undetermined".</summary>
    public const string UndeterminedValue = "und";

    private static readonly HashSet<string> KnownCodes = BuildKnownCodes();
    private static readonly Dictionary<string, string> KnownNames = BuildKnownNames();

    private LanguageCode(string value) => Value = value;

    public string Value { get; }

    public static LanguageCode English { get; } = new(EnglishValue);

    public bool IsEnglish => Value == EnglishValue;

    public override string ToString() => Value;

    /// <summary>
    /// Parses a language code out of arbitrary text. Tolerates the shapes a language model
    /// realistically produces — "pl", "PL", "pl\n", "Polish", "The language code is: pl." —
    /// and rejects everything it cannot map to a real language.
    /// </summary>
    public static bool TryParse(string? candidate, out LanguageCode languageCode)
    {
        languageCode = default;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var tokens = Tokenize(candidate).ToArray();

        // A bare answer ("pl", "Polish") is the common case and the only unambiguous one.
        if (tokens.Length == 1 && TryMatch(tokens[0], out languageCode))
        {
            return true;
        }

        // Otherwise scan from the end: models put the answer last ("The language code is: pl").
        // Scanning forward would match "is" -- a real ISO 639-1 code for Icelandic -- in that
        // very sentence, which is exactly the class of bug this parser exists to stop.
        for (var i = tokens.Length - 1; i >= 0; i--)
        {
            if (TryMatch(tokens[i], out languageCode))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryMatch(string token, out LanguageCode languageCode)
    {
        if (KnownCodes.Contains(token))
        {
            languageCode = new LanguageCode(token);
            return true;
        }

        if (KnownNames.TryGetValue(token, out var codeForName))
        {
            languageCode = new LanguageCode(codeForName);
            return true;
        }

        languageCode = default;
        return false;
    }

    private static IEnumerable<string> Tokenize(string candidate)
    {
        var tokens = candidate.Split(
            [' ', '\t', '\r', '\n', '.', ',', ':', ';', '"', '\'', '`', '(', ')', '[', ']', '{', '}', '-', '_', '*'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            if (token.All(char.IsLetter))
            {
                yield return token.ToLowerInvariant();
            }
        }
    }

    private static HashSet<string> BuildKnownCodes()
    {
        var codes = new HashSet<string>(StringComparer.Ordinal)
        {
            // Kept explicitly so detection still works if the process runs in globalization-invariant
            // mode, where GetCultures returns nothing but the invariant culture.
            "en", "pl", "de", "es", "fr", "it", "pt", "nl", "cs", "sk", "uk", "ru", "sv", "no", "da", "fi"
        };

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
        {
            var code = culture.TwoLetterISOLanguageName;

            if (code.Length == 2)
            {
                codes.Add(code);
            }
        }

        return codes;
    }

    private static Dictionary<string, string> BuildKnownNames()
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["english"] = "en",
            ["polish"] = "pl"
        };

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
        {
            var code = culture.TwoLetterISOLanguageName;
            var englishName = culture.EnglishName;
            var parenthesis = englishName.IndexOf('(');

            if (parenthesis > 0)
            {
                englishName = englishName[..parenthesis];
            }

            englishName = englishName.Trim().ToLowerInvariant();

            if (code.Length == 2 && englishName.Length > 0 && englishName.All(char.IsLetter))
            {
                names.TryAdd(englishName, code);
            }
        }

        return names;
    }
}

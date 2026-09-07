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

        // Fail closed. The model is told to answer with the bare code, so only two shapes are
        // accepted: a single token ("pl", "Polish"), or a label/value line whose value after the
        // last colon is a single token ("The language code is: pl").
        //
        // Free-running prose is rejected outright. Scanning its tokens is what made
        // "I cannot determine the language; perhaps Polish" read as a confident "pl", and
        // scanning them the other way round made "The language code is: pl" read as "is" --
        // a real ISO 639-1 code for Icelandic.
        var value = candidate;
        var lastColon = value.LastIndexOf(':');

        if (lastColon >= 0)
        {
            value = value[(lastColon + 1)..];
        }

        var tokens = Tokenize(value).ToArray();

        return tokens.Length == 1 && TryMatch(tokens[0], out languageCode);
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
            if (string.IsNullOrEmpty(culture.Name))
            {
                // The invariant culture reports "iv", which is not an ISO 639-1 code.
                continue;
            }

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
            if (string.IsNullOrEmpty(culture.Name))
            {
                continue;
            }

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

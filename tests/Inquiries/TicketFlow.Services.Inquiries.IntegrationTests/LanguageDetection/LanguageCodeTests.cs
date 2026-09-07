using FluentAssertions;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using Xunit;

namespace TicketFlow.Services.Inquiries.IntegrationTests.LanguageDetection;

public class LanguageCodeTests
{
    [Theory]
    [InlineData("pl", "pl")]
    [InlineData("PL", "pl")]
    [InlineData("pl\n", "pl")]
    [InlineData("  en  ", "en")]
    [InlineData("'pl'", "pl")]
    [InlineData("Polish", "pl")]
    [InlineData("The language code is: pl.", "pl")]
    [InlineData("Language: German", "de")]
    public void TryParse_accepts_the_shapes_a_model_actually_returns(string candidate, string expected)
    {
        var parsed = LanguageCode.TryParse(candidate, out var languageCode);

        parsed.Should().BeTrue();
        languageCode.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("42")]
    [InlineData("I'm sorry, I cannot help with that request")]
    [InlineData("xx")]
    // Prose is rejected outright: a trailing language name must not read as a confident answer.
    [InlineData("I cannot determine the language; perhaps Polish")]
    [InlineData("Detected language is probably German")]
    // A refusal with a colon must not read as a label introducing a value.
    [InlineData("I cannot determine: Polish")]
    [InlineData("Unable to classify: German")]
    // The invariant culture reports "iv", which is not an ISO 639-1 code.
    [InlineData("iv")]
    public void TryParse_rejects_anything_it_cannot_map_to_a_language(string? candidate)
    {
        var parsed = LanguageCode.TryParse(candidate, out _);

        parsed.Should().BeFalse();
    }

    [Fact]
    public void English_is_recognised_as_english()
    {
        LanguageCode.TryParse("en", out var languageCode).Should().BeTrue();

        languageCode.IsEnglish.Should().BeTrue();
        languageCode.Should().Be(LanguageCode.English);
    }

    [Fact]
    public void A_non_english_code_is_not_english()
    {
        LanguageCode.TryParse("pl", out var languageCode).Should().BeTrue();

        languageCode.IsEnglish.Should().BeFalse();
    }
}

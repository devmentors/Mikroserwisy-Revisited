using FluentAssertions;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using Xunit;

namespace TicketFlow.Services.Inquiries.IntegrationTests.LanguageDetection;

public class TranslationPolicyTests
{
    [Fact]
    public void A_detected_non_english_language_is_translated()
    {
        LanguageCode.TryParse("pl", out var polish).Should().BeTrue();

        TranslationPolicy.ShouldTranslate(LanguageDetectionResult.Detected(polish)).Should().BeTrue();
    }

    [Fact]
    public void English_is_not_translated()
    {
        TranslationPolicy.ShouldTranslate(LanguageDetectionResult.Detected(LanguageCode.English)).Should().BeFalse();
    }

    [Fact]
    public void An_unrecognised_response_does_not_trigger_a_translation()
    {
        TranslationPolicy.ShouldTranslate(LanguageDetectionResult.Unrecognized).Should().BeFalse();
    }

    [Fact]
    public void A_failed_detection_does_not_trigger_a_translation()
    {
        TranslationPolicy.ShouldTranslate(LanguageDetectionResult.Failed).Should().BeFalse();
    }
}

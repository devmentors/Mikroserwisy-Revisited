namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

public interface ILanguageDetector
{
    Task<string> GetTextLanguageCode(string text, CancellationToken cancellationToken = default);
}
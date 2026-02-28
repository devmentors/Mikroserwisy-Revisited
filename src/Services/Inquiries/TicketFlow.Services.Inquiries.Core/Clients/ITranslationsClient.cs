namespace TicketFlow.Services.Inquiries.Core.Clients;

public interface ITranslationsClient
{
    Task<string> TranslateAsync(string text, CancellationToken cancellationToken = default);
}

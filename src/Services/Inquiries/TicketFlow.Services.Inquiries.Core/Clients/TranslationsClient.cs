using System.Net.Http.Json;

namespace TicketFlow.Services.Inquiries.Core.Clients;

public sealed class TranslationsClient(HttpClient httpClient) : ITranslationsClient
{
    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/translations", new { Text = text }, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<string>(cancellationToken) ?? string.Empty;
    }
}

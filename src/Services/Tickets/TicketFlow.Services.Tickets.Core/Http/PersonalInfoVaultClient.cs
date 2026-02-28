using System.Net.Http.Json;

namespace TicketFlow.Services.Tickets.Core.Http;

internal sealed class PersonalInfoVaultClient(HttpClient httpClient) : IPersonalInfoVaultClient
{
    public async Task<PersonalInfoDto?> GetAsync(string personToken, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/personal-info/{personToken}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PersonalInfoDto>(cancellationToken);
    }

    public async Task<List<PersonalInfoDto>> GetBatchAsync(List<string> tokens, CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return [];
        }

        var response = await httpClient.PostAsJsonAsync("/personal-info/batch", new { Tokens = tokens }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BatchResponse>(cancellationToken);
        return result?.Items ?? [];
    }

    private record BatchResponse(List<PersonalInfoDto> Items);
}

using System.Net.Http.Json;

namespace TicketFlow.BFF.Http.PersonalInfoVault;

internal class PersonalInfoVaultClient : IPersonalInfoVaultClient
{
    private readonly HttpClient _httpClient;

    public PersonalInfoVaultClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<SearchPersonalInfoDto> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<SearchPersonalInfoDto>(
            $"/personal-info/search?query={Uri.EscapeDataString(query)}&limit={limit}",
            cancellationToken);
        return response ?? new SearchPersonalInfoDto([]);
    }
}

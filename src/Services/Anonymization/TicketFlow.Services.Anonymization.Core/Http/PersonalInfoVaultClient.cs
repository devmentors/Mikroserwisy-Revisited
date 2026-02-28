using System.Net.Http.Json;

namespace TicketFlow.Services.Anonymization.Core.Http;

internal sealed class PersonalInfoVaultClient(HttpClient httpClient) : IPersonalInfoVaultClient
{
    public async Task<string?> GetPersonTokenByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/personal-info/by-email/{Uri.EscapeDataString(email)}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<PersonalInfoDto>(cancellationToken);
        return result?.PersonToken;
    }
}

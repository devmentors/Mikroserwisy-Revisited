using System.Net.Http.Json;

namespace TicketFlow.Services.Inquiries.Core.Http;

public class PersonalInfoVaultClient(HttpClient httpClient) : IPersonalInfoVaultClient
{
    public async Task<string> StorePersonalInfoAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        var request = new { Name = name, Email = email };
        var response = await httpClient.PostAsJsonAsync("/personal-info", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StorePersonalInfoResponse>(cancellationToken);
        return result?.PersonToken ?? throw new InvalidOperationException("Failed to get PersonToken from Vault");
    }

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
            return new List<PersonalInfoDto>();
        }

        var request = new { Tokens = tokens };
        var response = await httpClient.PostAsJsonAsync("/personal-info/batch", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BatchResponse>(cancellationToken);
        return result?.Items ?? new List<PersonalInfoDto>();
    }

    private record StorePersonalInfoResponse(string PersonToken);
    private record BatchResponse(List<PersonalInfoDto> Items);
}

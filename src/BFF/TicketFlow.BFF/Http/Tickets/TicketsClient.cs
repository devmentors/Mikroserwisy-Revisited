using System.Net.Http.Json;

namespace TicketFlow.BFF.Http.Tickets;

internal class TicketsClient : ITicketsClient
{
    private readonly HttpClient _httpClient;

    public TicketsClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TicketsListDto?> GetTickets(int page = 1, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<TicketsListDto>($"/tickets?page={page}&limit={limit}", cancellationToken);
    }

    public async Task<List<AgentDto>> GetAgents(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AgentDto>>("/agents", cancellationToken) ?? [];
    }

    public async Task<TicketsListDto> GetTicketsByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/tickets/by-tokens", new { Tokens = tokens }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TicketsListDto>(cancellationToken: cancellationToken)
            ?? new TicketsListDto([], 0);
    }
}

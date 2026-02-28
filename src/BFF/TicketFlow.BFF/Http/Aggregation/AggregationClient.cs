using System.Net.Http.Json;

namespace TicketFlow.BFF.Http.Aggregation;

internal class AggregationClient : IAggregationClient
{
    private readonly HttpClient _httpClient;

    public AggregationClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<TicketProjectionDto>> GetProjectionsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<TicketProjectionDto>>("/projections", cancellationToken) ?? [];
    }

    public async Task<TicketStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<TicketStatisticsDto>("/statistics", cancellationToken)
            ?? new TicketStatisticsDto(0, 0, 0, 0, 0, 0);
    }

    public async Task<List<TicketProjectionDto>> GetProjectionsByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/projections/by-tokens", new { Tokens = tokens }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TicketProjectionDto>>(cancellationToken: cancellationToken) ?? [];
    }
}

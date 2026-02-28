using System.Net.Http.Json;
using TicketFlow.Shared.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TicketFlow.Services.SLA.Core.Http.Tickets;

internal class TicketsClient : ITicketsClient
{
    private readonly HttpClient _httpClient;
    
    public TicketsClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
    
    public async Task<TicketDto> GetTicketDetails(string ticketId, CancellationToken cancellationToken)
    {
        var json = await _httpClient.GetStringAsync($"/tickets/{ticketId}", cancellationToken);
        var result = JsonSerializer.Deserialize<TicketDto>(json, SerializationOptions.Default);
        return result;
    }

    public async Task<List<Guid>> GetSupervisorsUserIds(CancellationToken cancellationToken)
    {
        var allAgents = await _httpClient.GetFromJsonAsync<AgentDto[]>("agents", cancellationToken);
        return allAgents
            .Where(x => x.Position.Equals("Supervisor"))
            .Select(x => Guid.Parse(x.UserId))
            .ToList();
    }
}
using System.Net.Http.Json;

namespace TicketFlow.Services.Aggregation.Core.Http;

public class TicketsClient(HttpClient httpClient) : ITicketsClient
{
    public async Task<TicketDto> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TicketDto>($"/tickets/{ticketId}", cancellationToken);
    }

    public async Task<AgentDto> GetAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agents = await httpClient.GetFromJsonAsync<List<AgentDto>>("/agents", cancellationToken);
        return agents?.FirstOrDefault(a => a.Id == agentId.ToString());
    }
}

using TicketFlow.Shared.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TicketFlow.Services.Communication.Core.Http.Tickets;

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
}
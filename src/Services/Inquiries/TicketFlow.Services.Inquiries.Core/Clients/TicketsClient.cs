using System.Net.Http.Json;
using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;

namespace TicketFlow.Services.Inquiries.Core.Clients;

public sealed class TicketsClient(HttpClient httpClient) : ITicketsClient
{
    public async Task CreateTicketAsync(CreateTicketSynchronously command, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/tickets", command, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

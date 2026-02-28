using System.Net.Http.Json;

namespace TicketFlow.BFF.Http.Sla;

internal class SlaClient : ISlaClient
{
    private readonly HttpClient _httpClient;

    public SlaClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<DeadlineRemindersDto?> GetDeadlineReminders(string serviceType, string serviceSourceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DeadlineRemindersDto>(
                $"/sla/{serviceType}/{serviceSourceId}/deadline-reminders",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

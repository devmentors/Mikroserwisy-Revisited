using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketFlow.Services.Inquiries.Core.Queries;

namespace TicketFlow.Services.Inquiries.McpServer;

public class InquiriesApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public InquiriesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InquiriesListDto> ListInquiriesAsync(
        string? userId = null,
        int page = 1,
        int limit = 10,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/inquiries?page={page}&limit={limit}");

        if (!string.IsNullOrEmpty(userId))
        {
            request.Headers.Add("X-User-Id", userId);
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<InquiriesListDto>(JsonOptions, ct)
               ?? new InquiriesListDto([], 0);
    }

    public async Task<InquiriesListEntryDto?> GetInquiryByIdAsync(
        string inquiryId,
        string? userId = null,
        CancellationToken ct = default)
    {
        var list = await ListInquiriesAsync(userId, 1, 100, ct);
        return list.Data.FirstOrDefault(i => i.Id == inquiryId);
    }

    public async Task<InquiriesListEntryDto?> GetInquiryByTicketIdAsync(
        string ticketId,
        string? userId = null,
        CancellationToken ct = default)
    {
        var list = await ListInquiriesAsync(userId, 1, 100, ct);
        return list.Data.FirstOrDefault(i => i.TicketId == ticketId);
    }
}

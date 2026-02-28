using System.Net.Http.Json;

namespace TicketFlow.ClientsAPI.Http;

internal class InquiriesClient : IInquiriesClient
{
    private readonly HttpClient _httpClient;

    public InquiriesClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SubmitInquiryResponse> SubmitAsync(SubmitInquiryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/inquiries/submit", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return new SubmitInquiryResponse(Guid.NewGuid());
    }

    public async Task<InquiryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<InquiriesServiceListResponse>(
            "/inquiries?page=1&limit=1000",
            cancellationToken);

        var inquiry = response?.Data?.FirstOrDefault(i => i.Id == id);
        if (inquiry is null) return null;

        return MapToDto(inquiry);
    }

    public async Task<InquiriesListResponse> GetByEmailAsync(string email, int limit = 100, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<InquiriesServiceListResponse>(
            $"/inquiries?page=1&limit={limit}",
            cancellationToken);

        if (response is null)
            return new InquiriesListResponse([], 0);

        var filtered = response.Data
            .Where(i => i.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto)
            .ToList();

        return new InquiriesListResponse(filtered, filtered.Count);
    }

    private static InquiryDto MapToDto(InquiriesServiceEntryDto entry)
    {
        return new InquiryDto(
            entry.Id,
            entry.Name,
            entry.Email,
            entry.Title,
            entry.Description,
            entry.Category,
            entry.Status,
            entry.CreatedAt,
            entry.TicketId);
    }
}

namespace TicketFlow.ClientsAPI.Http;

public interface IInquiriesClient
{
    Task<SubmitInquiryResponse> SubmitAsync(SubmitInquiryRequest request, CancellationToken cancellationToken = default);
    Task<InquiryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InquiriesListResponse> GetByEmailAsync(string email, int limit = 100, CancellationToken cancellationToken = default);
}

public record SubmitInquiryRequest(string Name, string Email, string Title, string Description, string Category);
public record SubmitInquiryResponse(Guid Id);

public record InquiriesListResponse(List<InquiryDto> Data, int TotalCount);

public record InquiryDto(
    Guid Id,
    string Name,
    string Email,
    string Title,
    string Description,
    string Category,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? TicketId);

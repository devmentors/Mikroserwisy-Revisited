using TicketFlow.Services.Inquiries.Core.Data.Models;

namespace TicketFlow.Services.Inquiries.Core.Queries;

public record InquiriesListDto(List<InquiriesListEntryDto> Data, int TotalCount);

public record InquiriesListEntryDto(
    string Id,
    string Name,
    string Title,
    string Email,
    string Description,
    InquiryCategory Category,
    InquiryStatus Status,
    string CreatedAt,
    string? TicketId);
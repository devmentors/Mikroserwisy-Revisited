namespace TicketFlow.ClientsAPI.Http;

internal record InquiriesServiceListResponse(List<InquiriesServiceEntryDto> Data, int TotalCount);

internal record InquiriesServiceEntryDto(
    Guid Id,
    string Name,
    string Title,
    string Email,
    string Description,
    string Category,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? TicketId);

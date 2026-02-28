namespace TicketFlow.ClientsAPI.Contracts.V1;

public record InquiryV1Dto(
    Guid Id,
    string Title,
    string Status,
    string CreatedAt,
    string Name,
    string Email,
    string Description,
    string Category,
    Guid? TicketId);

public record InquiryListItemV1Dto(
    Guid Id,
    string Title,
    string Status,
    string CreatedAt);

public record InquiriesListV1Response(
    IEnumerable<InquiryListItemV1Dto> Data,
    int TotalCount);

public record InquiryCreatedV1Response(
    Guid Id,
    string Message);

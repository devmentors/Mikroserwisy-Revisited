namespace TicketFlow.ClientsAPI.Contracts.V2;

public record InquiryV2Dto(
    string Category,
    DateTimeOffset CreatedAt,
    string Description,
    string Email,
    TimeSpan EstimatedResponseTime,
    Guid Id,
    string Name,
    string Status,
    string SupportTier,
    Guid? TicketId,
    string Title);

public record InquiryListItemV2Dto(
    string Category,
    DateTimeOffset CreatedAt,
    Guid Id,
    string Status,
    string Title);

public record InquiriesListV2Response(
    IEnumerable<InquiryListItemV2Dto> Items,
    PaginationV2Dto Pagination);

public record PaginationV2Dto(
    int Page,
    int Limit,
    int Total);

public record InquiryCreatedV2Response(
    Guid Id,
    string Message,
    TimeSpan EstimatedResponseTime);

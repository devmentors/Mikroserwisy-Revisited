using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Inquiries.McpServer.Models;

/// <summary>
/// Response for list_my_inquiries tool with semantic context.
/// </summary>
public sealed record ListInquiriesResponse : SemanticResponse
{
    public required int TotalCount { get; init; }
    public required Dictionary<string, int> StatusBreakdown { get; init; }
    public IReadOnlyList<InquiryInfo>? RecentInquiries { get; init; }
    public required PaginationInfo Pagination { get; init; }
}

public sealed record InquiryInfo(
    string Id,
    string Title,
    string Status,
    string StatusDescription,
    string Category,
    string CreatedAt,
    string? TicketId
);

public sealed record PaginationInfo(int Page, int Limit, int TotalPages);

/// <summary>
/// Response for check_inquiry_status tool with status context.
/// </summary>
public sealed record InquiryStatusResponse : SemanticResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string StatusDescription { get; init; }
    public required string Category { get; init; }
    public required string CreatedAt { get; init; }
    public string? TicketId { get; init; }
    public bool HasTicket { get; init; }
}

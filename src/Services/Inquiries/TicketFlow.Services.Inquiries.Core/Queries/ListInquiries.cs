using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Inquiries.Core.Queries;

public record ListInquiries(
    int Page,
    int Limit,
    Guid? UserId = null
) : IQuery<InquiriesListDto>;
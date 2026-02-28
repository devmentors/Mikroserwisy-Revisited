using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Inquiries.Core.Queries;

public record ListInquiries(int Page, int Limit) : IQuery<InquiriesListDto>;
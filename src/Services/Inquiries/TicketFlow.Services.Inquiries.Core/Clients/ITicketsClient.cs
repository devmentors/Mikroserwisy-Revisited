using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;

namespace TicketFlow.Services.Inquiries.Core.Clients;

public interface ITicketsClient
{
    Task CreateTicketAsync(CreateTicketSynchronously command, CancellationToken cancellationToken = default);
}

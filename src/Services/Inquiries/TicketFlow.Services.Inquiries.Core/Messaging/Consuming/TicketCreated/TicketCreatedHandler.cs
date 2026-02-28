using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Messaging.Consuming.TicketCreated;

public class TicketCreatedHandler(IInquiriesRepository inquiriesRepository) : IMessageHandler<TicketCreated>
{
    public async Task HandleAsync(TicketCreated message, CancellationToken cancellationToken = default)
    {
        var inquiry = await inquiriesRepository.GetAsync(message.InquiryId, cancellationToken);

        if (inquiry == null)
        {
            throw new TicketFlowException("Inquiry not found");
        }
        
        inquiry.SetRelatedTicketId(message.Id);
        await inquiriesRepository.UpdateAsync(inquiry, cancellationToken);
    }
}
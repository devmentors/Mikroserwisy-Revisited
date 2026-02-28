using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Communication.Core.Validators;

public class MessageValidator
{
    public void Validate(Message message)
    {
        if (message.RecipentUserId is null && message.RecipentEmail is null)
        {
            throw new TicketFlowException("Either recipient's email or userId must be provided");
        }

        if (string.IsNullOrEmpty(message.Title))
        {
            throw new TicketFlowException("Title must be provided");
        }
        
        if (string.IsNullOrEmpty(message.Content))
        {
            throw new TicketFlowException("Content must be provided");
        }
    }
}
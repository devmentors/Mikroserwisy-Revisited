using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;

internal sealed class TranslationCompletedHandler(ITicketsRepository repository, ILogger<TranslationCompletedHandler> logger) 
    : IMessageHandler<TranslationCompleted>
{
    public async Task HandleAsync(TranslationCompleted message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Translation completed for ticket {message.ReferenceId}");

        var ticket = await repository.GetAsync(message.ReferenceId, cancellationToken);

        if (ticket is null)
        {
            var scheduledAction = new TicketScheduledAction(message.ReferenceId, message.TranslatedText);
            
            await repository.AddScheduledAction(scheduledAction, cancellationToken);
            logger.LogInformation($"Ticket {message.ReferenceId} was not found. Creating a scheduled action snapshot.");

            return;
        }
        
        ticket.SetTranslation(message.TranslatedText);
        ticket.SetBeforeQualification();
        
        await repository.UpdateAsync(ticket, cancellationToken);
        logger.LogInformation($"Ticket {message.ReferenceId} was found. Updated with translation.");
    }
}
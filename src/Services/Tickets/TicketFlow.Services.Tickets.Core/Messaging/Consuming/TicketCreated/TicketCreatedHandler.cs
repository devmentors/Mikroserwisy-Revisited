using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.TicketCreated;

public class TicketCreatedHandler(ITicketsRepository repository) : IMessageHandler<Publishing.TicketCreated>
{
    public async Task HandleAsync(Publishing.TicketCreated message, CancellationToken cancellationToken = default)
    {
        if (!FeatureFlags.UseListenToYourselfExample)
        {
            return;
        }
        
        var categoryValid = Enum.TryParse<TicketCategory>(message.Category, out var categoryParsed);
        if (categoryValid is false)
        {
            categoryParsed = TicketCategory.Other;
        }
        
        var ticket = new Ticket(message.Id, null, message.PersonToken, message.Title, message.Description, categoryParsed, message.LanguageCode);
        
        var scheduledAction = await repository.GetScheduledAction(message.Id, cancellationToken);

        if (scheduledAction is not null)
        {
            ticket.SetTranslation(scheduledAction.TranslatedText);
        }
        else if (ticket.IsEnglish is false)
        {
            ticket.WaitForScheduledActions();
        }
        
        await repository.AddAsync(ticket, cancellationToken);
    }
}
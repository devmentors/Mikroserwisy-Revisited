namespace TicketFlow.Services.Tickets.Core.Data.Models;

public class TicketScheduledAction
{
    public Guid Id { get; }
    public string TranslatedText { get; }

    private TicketScheduledAction()
    {
    }

    public TicketScheduledAction(Guid id, string translatedText)
    {
        Id = id;
        TranslatedText = translatedText;
    }
}
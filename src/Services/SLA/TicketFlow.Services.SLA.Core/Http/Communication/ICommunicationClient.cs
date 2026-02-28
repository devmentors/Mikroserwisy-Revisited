using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Http.Communication;

public interface ICommunicationClient
{
    Task SendReminderMessage(Guid userId, ServiceType serviceType, string serviceSourceId, ReminderMessageType type, CancellationToken cancellationToken);

    public enum ReminderMessageType
    {
        FirstReminder,
        SecondReminder,
        FinalReminder,
        SLABreachedRecurring
    }
}
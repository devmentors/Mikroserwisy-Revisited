using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data.Repositories;

public interface ISLARepository
{
    Task<SignedSLA?> GetSLAByRequestorDomain(string domain, CancellationToken cancellationToken);

    Task SaveReminders(DeadlineReminders reminders, CancellationToken cancellationToken);
    
    Task<DeadlineReminders?> GetRemindersFor(ServiceType serviceType, string serviceSourceId,
        CancellationToken cancellationToken);

    Task<DeadlineReminders> GetRemindersFor(ICollection<ServiceType> anyOfServiceTypes, string serviceSourceId,
        CancellationToken cancellationToken);

    Task<List<DeadlineReminders>> GetWithPendingFirstReminder(CancellationToken cancellationToken);
    Task<List<DeadlineReminders>> GetWithPendingSecondReminder(CancellationToken cancellationToken);
    Task<List<DeadlineReminders>> GetWithPendingFinalReminder(CancellationToken cancellationToken);
    Task<List<DeadlineReminders>> GetWithDeadlineDateBreached(CancellationToken cancellationToken);
}
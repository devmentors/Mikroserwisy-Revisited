using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Services.SLA.Core.Messaging.Consuming;
using TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

namespace TicketFlow.Services.SLA.Core.Data.Repositories;

internal sealed class SLARepository : ISLARepository, 
    IGetMessageRelatedEntityVersion<ITicketChange>,
    IGetMessageRelatedEntityVersion<TicketQualified>,
    IGetMessageRelatedEntityVersion<AgentAssignedToTicket>,
    IGetMessageRelatedEntityVersion<TicketResolved>
{
    private readonly SLADbContext _slaDbContext;

    public SLARepository(SLADbContext slaDbContext)
    {
        _slaDbContext = slaDbContext ?? throw new ArgumentNullException(nameof(slaDbContext));
    }
    
    public async Task<SignedSLA> GetSLAByRequestorDomain(string domain, CancellationToken cancellationToken)
    {
        return await _slaDbContext.SignedSLAs.SingleOrDefaultAsync(x => x.Domain == domain, cancellationToken);
    }

    public async Task SaveReminders(DeadlineReminders reminders, CancellationToken cancellationToken)
    {
        if (reminders.IsTransient)
        {
            await _slaDbContext.DeadlineReminders.AddAsync(reminders, cancellationToken);
        }
        else
        {
            _slaDbContext.DeadlineReminders.Update(reminders);
        }

        await _slaDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeadlineReminders> GetRemindersFor(ServiceType serviceType, string serviceSourceId, CancellationToken cancellationToken)
    {
        return await _slaDbContext.DeadlineReminders.SingleOrDefaultAsync(x => 
            x.ServiceType == serviceType && x.ServiceSourceId == serviceSourceId, cancellationToken);
    }

    public async Task<DeadlineReminders> GetRemindersFor(ICollection<ServiceType> anyOfServiceTypes, string serviceSourceId, CancellationToken cancellationToken)
    {
        return await _slaDbContext.DeadlineReminders.SingleOrDefaultAsync(x => 
            anyOfServiceTypes.Contains(x.ServiceType) && x.ServiceSourceId == serviceSourceId, cancellationToken);
    }

    public async Task<List<DeadlineReminders>> GetWithPendingFirstReminder(CancellationToken cancellationToken)
    {
        return await _slaDbContext.DeadlineReminders
            .Where(x => x.ServiceCompleted == false)
            .Where(x => x.FirstReminderSent == false)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadlineReminders>> GetWithPendingSecondReminder(CancellationToken cancellationToken)
    {
        return await _slaDbContext.DeadlineReminders
            .Where(x => x.ServiceCompleted == false)
            .Where(x => x.SecondReminderSent == false)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadlineReminders>> GetWithPendingFinalReminder(CancellationToken cancellationToken)
    {
        return await _slaDbContext.DeadlineReminders
            .Where(x => x.ServiceCompleted == false)
            .Where(x => x.FinalReminderSent == false)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeadlineReminders>> GetWithDeadlineDateBreached(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _slaDbContext.DeadlineReminders
            .Where(x => x.DeadlineDateUtc <= now)
            .Where(x => x.ServiceCompleted == false)
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetEntityVersionAsync(ITicketChange message, CancellationToken cancellationToken = default)
    {
        var matchingDeadline = await _slaDbContext.DeadlineReminders
            .Where(x =>
                x.ServiceType.Equals(ServiceType.QuestionTicket)
                || x.ServiceType.Equals(ServiceType.IncidentTicket))
            .Where(x => x.ServiceSourceId.Equals(message.TicketId.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
        
        return matchingDeadline?.ServiceLastKnownVersion;
    }

    public Task<int?> GetEntityVersionAsync(TicketQualified message, CancellationToken cancellationToken = default)
    {
        return GetEntityVersionAsync((ITicketChange)message, cancellationToken);
    }

    public Task<int?> GetEntityVersionAsync(AgentAssignedToTicket message, CancellationToken cancellationToken = default)
    {
        return GetEntityVersionAsync((ITicketChange)message, cancellationToken);
    }

    public Task<int?> GetEntityVersionAsync(TicketResolved message, CancellationToken cancellationToken = default)
    {
        return GetEntityVersionAsync((ITicketChange)message, cancellationToken);
    }
}
namespace TicketFlow.BFF.Http.Sla;

public interface ISlaClient
{
    Task<DeadlineRemindersDto?> GetDeadlineReminders(string serviceType, string serviceSourceId, CancellationToken cancellationToken = default);
}

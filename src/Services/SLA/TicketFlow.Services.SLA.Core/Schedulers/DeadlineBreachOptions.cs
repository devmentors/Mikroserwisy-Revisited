namespace TicketFlow.Services.SLA.Core.Schedulers;

public class DeadlineBreachOptions
{
    public int WatcherIntervalInSeconds { get; set; }
    public int SecondsBetweenBreachAlerts { get; set; }
}
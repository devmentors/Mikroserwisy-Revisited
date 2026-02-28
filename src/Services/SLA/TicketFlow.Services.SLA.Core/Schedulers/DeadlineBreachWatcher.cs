using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketFlow.Services.SLA.Core.Data.Repositories;
using TicketFlow.Services.SLA.Core.Http.Communication;
using TicketFlow.Services.SLA.Core.Messaging.Publishing;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Schedulers;

public class DeadlineBreachWatcher(
    DeadlineBreachOptions options,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            await Task.Delay(options.WatcherIntervalInSeconds, stoppingToken);
            try
            {
                await MarkBreachedDeadlines(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nameof(DeadlineBreachWatcher)} failed with exception: {ex}");
            }
        }
    }

    private async Task MarkBreachedDeadlines(CancellationToken stoppingToken)
    {
        using var iocScope = serviceProvider.CreateScope();
        var slaRepository = iocScope.ServiceProvider.GetRequiredService<ISLARepository>();
        var messagePublisher = iocScope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var communicationClient = iocScope.ServiceProvider.GetRequiredService<ICommunicationClient>();
        
        var overdueDeadlines = await slaRepository.GetWithDeadlineDateBreached(stoppingToken);
        foreach (var deadline in overdueDeadlines)
        {
            deadline.DetectDeadlineBreached();
            
            var lastBreachNotification = deadline.LastDeadlineBreachedAlertSentDateUtc ?? deadline.DeadlineDateUtc;
            var canAlertAgain = DateTimeOffset.UtcNow - lastBreachNotification > TimeSpan.FromSeconds(options.SecondsBetweenBreachAlerts);
            
            if (canAlertAgain)
            {
                deadline.MarkDeadlineBreachAlertSent();
                /* Explicit decision to use CancellationToken.None - email was already sent so it's better to "force save" */
                await messagePublisher.PublishAsync(new SLABreached(deadline.ServiceType, deadline.ServiceSourceId), cancellationToken: CancellationToken.None);

                if (deadline.UserIdToRemind.HasValue)
                {
                    await communicationClient.SendReminderMessage(
                        deadline.UserIdToRemind!.Value,
                        deadline.ServiceType,
                        deadline.ServiceSourceId,
                        ICommunicationClient.ReminderMessageType.SLABreachedRecurring,
                        stoppingToken);
                }
            }
            
            await slaRepository.SaveReminders(deadline, CancellationToken.None);
        }

        await Task.CompletedTask;
    }
}
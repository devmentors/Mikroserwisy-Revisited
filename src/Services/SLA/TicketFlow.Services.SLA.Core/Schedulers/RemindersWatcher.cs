using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Data.Repositories;
using TicketFlow.Services.SLA.Core.Http;
using TicketFlow.Services.SLA.Core.Http.Communication;

namespace TicketFlow.Services.SLA.Core.Schedulers;

public class RemindersWatcher(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            await Task.Delay(2_000, stoppingToken);
            try
            {
                await SendFirstReminders(stoppingToken);
                await SendSecondReminders(stoppingToken);
                await SendFinalReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                serviceProvider.GetRequiredService<ILogger<RemindersWatcher>>().LogError(ex, ex.Message);
            }
        }
    }

    private async Task SendFirstReminders(CancellationToken stoppingToken)
    {
        using var iocScope = serviceProvider.CreateScope();
        var slaRepository = iocScope.ServiceProvider.GetRequiredService<ISLARepository>();
        var communicationClient = iocScope.ServiceProvider.GetRequiredService<ICommunicationClient>();
        
        var pendingReminders = await slaRepository.GetWithPendingFirstReminder(stoppingToken);
        foreach (var reminders in pendingReminders.Where(x => x.UserIdToRemind is not null))
        {
            await communicationClient.SendReminderMessage(
                reminders.UserIdToRemind!.Value, 
                reminders.ServiceType,
                reminders.ServiceSourceId,
                ICommunicationClient.ReminderMessageType.FirstReminder,
                stoppingToken);
            
            reminders.MarkFirstReminderSent();
            /* Explicit decision to use CancellationToken.None - email was already sent so it's better to "force save" */
            await slaRepository.SaveReminders(reminders, CancellationToken.None);
        }
    }

    private async Task SendSecondReminders(CancellationToken stoppingToken)
    {
        using var iocScope = serviceProvider.CreateScope();
        var slaRepository = iocScope.ServiceProvider.GetRequiredService<ISLARepository>();
        var communicationClient = iocScope.ServiceProvider.GetRequiredService<ICommunicationClient>();

        var pendingReminders = await slaRepository.GetWithPendingSecondReminder(stoppingToken);
        foreach (var reminders in pendingReminders.Where(x => x.UserIdToRemind is not null))
        {
            await communicationClient.SendReminderMessage(
                reminders.UserIdToRemind!.Value, 
                reminders.ServiceType,
                reminders.ServiceSourceId,
                ICommunicationClient.ReminderMessageType.SecondReminder,
                stoppingToken);
            
            reminders.MarkSecondReminderSent();
            /* Explicit decision to use CancellationToken.None - email was already sent so it's better to "force save" */
            await slaRepository.SaveReminders(reminders, CancellationToken.None);
        }
    }

    private async Task SendFinalReminders(CancellationToken stoppingToken)
    {
        using var iocScope = serviceProvider.CreateScope();
        var slaRepository = iocScope.ServiceProvider.GetRequiredService<ISLARepository>();
        var communicationClient = iocScope.ServiceProvider.GetRequiredService<ICommunicationClient>();
        
        var pendingReminders = await slaRepository.GetWithPendingFinalReminder(stoppingToken);
        foreach (var reminders in pendingReminders.Where(x => x.UserIdToRemind is not null))
        {
            await communicationClient.SendReminderMessage(
                reminders.UserIdToRemind!.Value, 
                reminders.ServiceType,
                reminders.ServiceSourceId,
                ICommunicationClient.ReminderMessageType.FinalReminder,
                stoppingToken);
            
            reminders.MarkFinalReminderSent();
            /* Explicit decision to use CancellationToken.None - email was already sent so it's better to "force save" */
            await slaRepository.SaveReminders(reminders, CancellationToken.None);
        }
    }
}
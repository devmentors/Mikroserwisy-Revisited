using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Http.Communication;

internal class CommunicationsClient(HttpClient httpClient, ILogger<CommunicationsClient> logger) : ICommunicationClient
{
    public async Task SendReminderMessage(Guid userId, ServiceType serviceType, string serviceSourceId, ICommunicationClient.ReminderMessageType type,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"Sending reminder message for user {userId}, service type {serviceType}, service source {serviceSourceId}, reminder type {type}");
        var message = CreateForReminder(userId, serviceType, serviceSourceId, type);
        
        await httpClient.PostAsJsonAsync("/messages/", message, cancellationToken);
    }

    private MessageDto CreateForReminder(Guid userId, ServiceType serviceType, string serviceSourceId,
        ICommunicationClient.ReminderMessageType type)
    {
        var reminderTitle = type switch
        {
            ICommunicationClient.ReminderMessageType.FirstReminder => $"Przypomnienie #1",
            ICommunicationClient.ReminderMessageType.SecondReminder => $"Przypomnienie #2",
            ICommunicationClient.ReminderMessageType.FinalReminder => $"Ostateczne przypomnienie",
            ICommunicationClient.ReminderMessageType.SLABreachedRecurring => $"SLA niespełnione",
        };
        
        var reminderContent = type switch
        {
            ICommunicationClient.ReminderMessageType.FirstReminder => $"{serviceType.ToHumanReadableString()} - ID: {serviceSourceId} jest przypisany do Ciebie i czeka na twoją akcje",
            ICommunicationClient.ReminderMessageType.SecondReminder => $"{serviceType.ToHumanReadableString()} - ID: {serviceSourceId} jest przypisany do Ciebie i czeka na twoją akcje",
            ICommunicationClient.ReminderMessageType.FinalReminder => $"{serviceType.ToHumanReadableString()} - ID: {serviceSourceId} zaraz przekroczy SLA - działaj jak najszybciej!",
            ICommunicationClient.ReminderMessageType.SLABreachedRecurring => $"{serviceType.ToHumanReadableString()} - ID: {serviceSourceId} przekroczyło ustalone SLA - działaj!",
        };

        return new MessageDto
        {
            RecipentEmail = null,
            RecipentUserId = userId,
            SenderUserId = null, // SYSTEM
            Title = reminderTitle,
            Content = reminderContent,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
namespace TicketFlow.Services.SLA.Core.Data.Models;

public class DeadlineReminders
{
    private DeadlineReminders(){}
    
    public DeadlineReminders(
        ServiceType serviceType,
        string serviceSourceId,
        Guid? userIdToRemind,
        DateTimeOffset serviceRequestDateUtc,
        CalculatedDeadline calculatedDeadline)
    {
        ServiceType = serviceType;
        ServiceSourceId = serviceSourceId;
        UserIdToRemind = userIdToRemind;
        CalculateReminders(serviceRequestDateUtc, calculatedDeadline);
    }

    public Guid Id { get; init; }
    public bool IsTransient => Id.Equals(Guid.Empty);
    public ServiceType ServiceType { get; init; }
    public string ServiceSourceId { get; init; }
    public int ServiceLastKnownVersion { get; set; }
    public Guid? UserIdToRemind { get; set; }
    public DateTimeOffset? FirstReminderDateUtc { get; private set; }
    public bool FirstReminderSent { get; private set; }
    public DateTimeOffset? SecondReminderDateUtc { get; private set; }
    public bool SecondReminderSent { get; private set; }
    public DateTimeOffset? FinalReminderDateUtc { get; private set; }
    public bool FinalReminderSent { get; private set; }
    public bool? DeadlineMet { get; private set; }
    public bool ServiceCompleted { get; private set; }
    public DateTimeOffset DeadlineDateUtc { get; private set; }
    public DateTimeOffset? LastDeadlineBreachedAlertSentDateUtc { get; private set; }

    private void CalculateReminders(DateTimeOffset serviceRequestDateUtc, CalculatedDeadline calculatedDeadline)
    {
        if (calculatedDeadline is null)
        {
            throw new ArgumentNullException(nameof(calculatedDeadline));
        }
        
        var dueDate = calculatedDeadline.DueDateUtc;
        DeadlineDateUtc = dueDate;
        
        if (dueDate <= DateTimeOffset.UtcNow.AddHours(1))
        {
            FirstReminderDateUtc = serviceRequestDateUtc.AddMinutes(15);
            SecondReminderDateUtc = serviceRequestDateUtc.AddMinutes(30);
            FinalReminderDateUtc = serviceRequestDateUtc.AddMinutes(45);
            return;
        }
        
        FirstReminderDateUtc = serviceRequestDateUtc.AddMinutes(15);
        var middleTime = (dueDate - serviceRequestDateUtc)/2;
        SecondReminderDateUtc = serviceRequestDateUtc.Add(middleTime);
        FinalReminderDateUtc = serviceRequestDateUtc.AddMinutes(-15);
    }
    
    public void UpdateFromServiceChange(string status)
    {
        if (IsFinishingStatus(status))
        {
            DeadlineMet = DateTimeOffset.UtcNow > DeadlineDateUtc;
            ServiceCompleted = true;
        }
        else
        {
            ServiceCompleted = false;
        }
    }

    public void DetectDeadlineBreached()
    {
        var deadlineBreached = DateTimeOffset.UtcNow > DeadlineDateUtc;
        if (deadlineBreached)
        {
            DeadlineMet = false;
        }
    }

    public void MarkFirstReminderSent()
    {
        FirstReminderSent = true;
    }

    public void MarkSecondReminderSent()
    {
        SecondReminderSent = true;
    }

    public void MarkFinalReminderSent()
    {
        FinalReminderSent = true;
    }

    public void MarkDeadlineBreachAlertSent()
    {
        if (DeadlineMet is null)
        {
            throw new InvalidOperationException("DeadlineMet is null");
        }
        
        LastDeadlineBreachedAlertSentDateUtc = DateTimeOffset.UtcNow;
    }

    private bool IsFinishingStatus(string status)
    {
        switch (ServiceType)
        {
            case ServiceType.IncidentTicket:
            case ServiceType.QuestionTicket:
                return TicketStatus.IsFinishingForTicket(status);
        }
        
        throw new NotImplementedException(ServiceType.ToString());
    }
}
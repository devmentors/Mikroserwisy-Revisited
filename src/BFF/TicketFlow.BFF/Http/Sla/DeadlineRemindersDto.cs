namespace TicketFlow.BFF.Http.Sla;

public record DeadlineRemindersDto(
    Guid Id,
    string ServiceType,
    string ServiceSourceId,
    int ServiceLastKnownVersion,
    Guid? UserIdToRemind,
    DateTimeOffset? FirstReminderDateUtc,
    bool FirstReminderSent,
    DateTimeOffset? SecondReminderDateUtc,
    bool SecondReminderSent,
    DateTimeOffset? FinalReminderDateUtc,
    bool FinalReminderSent,
    bool? DeadlineMet,
    bool ServiceCompleted,
    DateTimeOffset DeadlineDateUtc,
    DateTimeOffset? LastDeadlineBreachedAlertSentDateUtc);

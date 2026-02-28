namespace TicketFlow.Services.Communication.Core.ExternalServices.Email;

public record EmailMessage(
    string To,
    string Subject,
    string Body,
    EmailPriority Priority = EmailPriority.Normal,
    string? From = null,
    List<string>? Cc = null,
    List<string>? Bcc = null);

public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public record EmailSendResult(
    bool Success,
    string? MessageId,
    EmailServiceErrorCode ErrorCode,
    string? ErrorMessage);

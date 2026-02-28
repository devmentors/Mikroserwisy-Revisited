namespace TicketFlow.Services.Communication.Core.ExternalServices.Email;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

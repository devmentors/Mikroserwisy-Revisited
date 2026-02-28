using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Core.Messages;

public interface IMessageService
{
    Task SaveMessageAsync(Message message, string? recipientLanguageCode = null, CancellationToken ct = default);
}

using TicketFlow.Services.Communication.Core.Data;
using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Communication.Core.Messaging.Consuming;

public class AlertsHandler(CommunicationDbContext dbContext) : IMessageHandler<ProducerAgnosticAlertMessage>
{
    public async Task HandleAsync(ProducerAgnosticAlertMessage message, CancellationToken cancellationToken = default)
    {
        var alert = new Alert
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Type = message.AlertType,
            Message = message.AlertMessageContent
        };
        
        await dbContext.AddAsync(alert, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
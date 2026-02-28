using Microsoft.Extensions.Logging;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Anonymization.Core.Messaging.Consuming.Handlers;

public class AnonymizationCompletedHandler(
    IAnonymizationRepository repository,
    ILogger<AnonymizationCompletedHandler> logger) : IMessageHandler<AnonymizationCompleted>
{
    public async Task HandleAsync(AnonymizationCompleted message, CancellationToken cancellationToken = default)
    {
        var request = await repository.GetByIdAsync(message.RequestId, cancellationToken);
        if (request is null)
        {
            logger.LogWarning("Anonymization request {RequestId} not found", message.RequestId);
            return;
        }

        request.MarkServiceCompleted(message.ServiceName, message.Success, message.ErrorMessage);
        await repository.UpdateAsync(request, cancellationToken);

        logger.LogInformation("Service {ServiceName} completed for request {RequestId}. Success: {Success}",
            message.ServiceName, message.RequestId, message.Success);
    }
}

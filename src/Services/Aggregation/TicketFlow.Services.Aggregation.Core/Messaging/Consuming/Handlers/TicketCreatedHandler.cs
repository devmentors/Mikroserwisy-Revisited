using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Models;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class TicketCreatedHandler(
    ITicketProjectionRepository repository,
    ILogger<TicketCreatedHandler> logger) : IMessageHandler<TicketCreated>
{
    public async Task HandleAsync(TicketCreated message, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(message.Id, cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug("TicketProjection {TicketId} already exists, skipping", message.Id);
            return;
        }

        var projection = TicketProjection.Create(
            message.Id,
            message.InquiryId,
            message.PersonToken,
            message.Title,
            message.Description,
            message.Category,
            message.LanguageCode,
            message.Version);

        await repository.AddAsync(projection, cancellationToken);
        logger.LogInformation("Created TicketProjection for ticket {TicketId}", message.Id);
    }
}

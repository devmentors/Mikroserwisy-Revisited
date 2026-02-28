using Microsoft.Extensions.Logging;
using TicketFlow.Services.Anonymization.Core.Data.Models;
using TicketFlow.Services.Anonymization.Core.Exceptions;
using TicketFlow.Services.Anonymization.Core.Messaging.Publishing;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Anonymization.Core.Commands.CreateAnonymizationRequest;

internal sealed class CreateAnonymizationRequestHandler(
    IAnonymizationRepository repository,
    IMessagePublisher messagePublisher,
    ILogger<CreateAnonymizationRequestHandler> logger) : ICommandHandler<CreateAnonymizationRequest>
{
    public async Task HandleAsync(
        CreateAnonymizationRequest command,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByPersonTokenAsync(command.PersonToken, cancellationToken);
        if (existing is not null && existing.Status == AnonymizationStatus.InProgress)
        {
            throw new AnonymizationConflictException(command.PersonToken, existing.Id);
        }

        var request = new AnonymizationRequest(
            command.RequestId,
            command.PersonToken,
            command.RequestedByEmail ?? "admin@ticketflow.com");
        await repository.AddAsync(request, cancellationToken);

        var anonymizationEvent = new AnonymizationRequested(
            request.Id,
            command.PersonToken,
            DateTimeOffset.UtcNow);

        await messagePublisher.PublishAsync(
            anonymizationEvent,
            destination: "anonymization-exchange",
            routingKey: "anonymization-requested",
            cancellationToken: cancellationToken);

        logger.LogInformation("Started anonymization request {RequestId} for person {PersonToken}", request.Id, command.PersonToken);
    }
}

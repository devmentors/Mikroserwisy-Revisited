using Microsoft.Extensions.Logging;
using TicketFlow.Services.Anonymization.Core.Data.Models;
using TicketFlow.Services.Anonymization.Core.Exceptions;
using TicketFlow.Services.Anonymization.Core.Http;
using TicketFlow.Services.Anonymization.Core.Messaging.Publishing;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Anonymization.Core.Commands.CreateAnonymizationRequest;

internal sealed class CreateAnonymizationRequestHandler(
    IAnonymizationRepository repository,
    IMessagePublisher messagePublisher,
    IPersonalInfoVaultClient vaultClient,
    ILogger<CreateAnonymizationRequestHandler> logger) : ICommandHandler<CreateAnonymizationRequest>
{
    public async Task HandleAsync(
        CreateAnonymizationRequest command,
        CancellationToken cancellationToken = default)
    {
        var personToken = command.PersonToken;

        // Resolve personToken from email if needed
        if (string.IsNullOrEmpty(personToken) && !string.IsNullOrEmpty(command.Email))
        {
            personToken = await vaultClient.GetPersonTokenByEmailAsync(command.Email, cancellationToken);
            if (string.IsNullOrEmpty(personToken))
            {
                throw new PersonNotFoundException(command.Email);
            }
        }

        if (string.IsNullOrEmpty(personToken))
        {
            throw new PersonTokenOrEmailRequiredException();
        }

        var anonymizationRequest = await repository.GetByPersonTokenAsync(personToken, cancellationToken);
        if (anonymizationRequest?.Status is AnonymizationStatus.InProgress)
        {
            throw new AnonymizationConflictException(personToken, anonymizationRequest.Id);
        }

        var request = new AnonymizationRequest(
            command.RequestId,
            personToken,
            command.RequestedByEmail ?? "admin@ticketflow.com");
        
        await repository.AddAsync(request, cancellationToken);

        var anonymizationEvent = new AnonymizationRequested(
            request.Id,
            personToken,
            DateTimeOffset.UtcNow);

        await messagePublisher.PublishAsync(
            anonymizationEvent,
            destination: "anonymization-exchange",
            routingKey: "anonymization-requested",
            cancellationToken: cancellationToken);

        logger.LogInformation("Started anonymization request {RequestId} for person {PersonToken}", request.Id, personToken);
    }
}

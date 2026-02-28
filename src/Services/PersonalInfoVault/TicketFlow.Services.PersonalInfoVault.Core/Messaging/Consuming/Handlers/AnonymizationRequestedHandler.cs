using Microsoft.Extensions.Logging;
using TicketFlow.Services.PersonalInfoVault.Core.Messaging.Publishing;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.PersonalInfoVault.Core.Messaging.Consuming.Handlers;

public class AnonymizationRequestedHandler(
    IPersonalInfoRepository repository,
    IMessagePublisher messagePublisher,
    ILogger<AnonymizationRequestedHandler> logger) : IMessageHandler<AnonymizationRequested>
{
    public async Task HandleAsync(AnonymizationRequested message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received AnonymizationRequested for person {PersonToken}", message.PersonToken);

        var success = false;
        string? errorMessage = null;

        try
        {
            var personalInfo = await repository.GetByTokenAsync(message.PersonToken, cancellationToken);
            if (personalInfo is null)
            {
                logger.LogWarning("PersonalInfo not found for token {PersonToken}", message.PersonToken);
                errorMessage = "PersonalInfo not found";
            }
            else if (personalInfo.IsAnonymized)
            {
                logger.LogInformation("PersonalInfo already anonymized for token {PersonToken}", message.PersonToken);
                success = true;
            }
            else
            {
                personalInfo.Anonymize();
                await repository.UpdateAsync(personalInfo, cancellationToken);
                logger.LogInformation("PersonalInfo anonymized for token {PersonToken}", message.PersonToken);
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to anonymize PersonalInfo for token {PersonToken}", message.PersonToken);
            errorMessage = ex.Message;
        }

        var completedEvent = new AnonymizationCompleted(
            message.RequestId,
            ServiceName: "vault",
            Success: success,
            ErrorMessage: errorMessage);

        await messagePublisher.PublishAsync(
            completedEvent,
            destination: VaultTopologyInitializer.VaultExchange,
            routingKey: "vault.anonymization.completed",
            cancellationToken: cancellationToken);

        logger.LogInformation("Anonymization completed for request {RequestId}. Success: {Success}",
            message.RequestId, success);
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.PersonalInfoVault.Core.Messaging.Consuming;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.PersonalInfoVault.Core.Messaging;

public class VaultConsumerService(
    IMessageConsumer messageConsumer,
    ILogger<VaultConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageConsumer.ConsumeMessage<AnonymizationRequested>(
            queue: VaultTopologyInitializer.AnonymizationQueue,
            cancellationToken: stoppingToken);

        logger.LogInformation("Started consuming events from anonymization-exchange");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

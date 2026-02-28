using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.AnomalyGeneration.MessagingApi;

public class AnomalySynchronizationConfigurator(
    IAnomaliesStorage storage,
    IMessageConsumer messageConsumer,
    IOptions<AppOptions> appOptions,
    ILogger<AnomalySynchronizationConfigurator> logger)
{
    public async Task ConsumeAnomalyChanges()
    {
        await messageConsumer
            .ConsumeMessage<AnomalyEventWrapper>(
                handle: message =>
                {
                    var isAnomalyEnabled = message.Enabled is not null;
                    
                    if (isAnomalyEnabled)
                    {
                        storage.EnableAnomaly(new EnableAnomalyRequest(
                            message.Enabled!.AnomalyType, 
                            message.Enabled!.MessageType, 
                            message.Enabled!.AdditionalParams));
                    }
                    else
                    {
                        storage.DisableAnomaly(
                            message.Disabled!.AnomalyType, 
                            message.Disabled!.MessageType);
                    }
                    
                    return Task.CompletedTask;
                },
                queue: AnomalyTopologyBuilder.AnomaliesAppExclusiveQueuePrefix(appOptions.Value),
                acceptedMessageTypes: null, /* All of them */
                cancellationToken: CancellationToken.None);
    }
}
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox;

namespace TicketFlow.Shared.AnomalyGeneration;

public static class Extensions
{
    public static IMessagingRegisterer UseAnomalies(this IMessagingRegisterer registerer)
    {
        var generator = new AnomalyGenerator();
        registerer.Services.AddSingleton(generator);
        
        registerer.Services.AddSingleton<IConsumerAnomalyGenerator>(generator);
        registerer.Services.AddSingleton<IOutboxAnomalyGenerator>(generator);
        registerer.Services.AddSingleton<IProducerAnomalyGenerator>(generator);
        registerer.Services.AddSingleton<IAnomaliesStorage>(generator);
        
        registerer.Services.AddScoped<IMessageExecutionStep, AnomalyBeforeExecutionSteps>();
        registerer.Services.AddScoped<IMessageExecutionStep, AnomalyWithinTransactionExecutionStep>();
        registerer.Services.AddScoped<IMessageExecutionStep, AnomalyAfterExecutionStep>();

        registerer.Services.TryDecorate(typeof(IMessageHandler<>), typeof(MessageHandlerAnomalyDecorator<>));
        registerer.Services.TryDecorate(typeof(IMessageOutbox), typeof(MessageOutboxAnomalyDecorator));
        registerer.Services.TryDecorate(typeof(IMessagePublisher), typeof(MessagePublisherAnomalyDecorator));

        registerer.Services.AddSingleton<AnomalySynchronizationConfigurator>();
        registerer.Services.AddSingleton<AnomalyContextAccessor>();
        
        return registerer;
    }
}
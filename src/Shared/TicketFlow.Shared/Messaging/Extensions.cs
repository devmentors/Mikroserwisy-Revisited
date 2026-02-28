using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging;

public static class Extensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration,
        Action<IMessagingRegisterer> register)
    {
        var registerer = new MessagingRegisterer(services, configuration);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        services.Scan(x => x.FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IMessageHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        
        register(registerer); //Required here because otherwise won't find IMessageHandler<> to decorate

        services.AddSingleton<MessagePropertiesAccessor>();
        services.AddSingleton<TopologyReadinessAccessor>();
        registerer.Services.AddScoped<IMessageExecutor, MessageExecutor>();
        registerer.Services.TryDecorate(typeof(ICommandHandler<>), typeof(CommandHandlerExecutorDecorator<>));
        registerer.Services.TryDecorate(typeof(IMessageHandler<>), typeof(MessageHandlerExecutorDecorator<>));
        
        return services;
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

public static class Extensions
{
    public const string SectionName = "OutOfOrderDetection";
    
    public static IMessagingRegisterer UseOutOfOrderDetection(this IMessagingRegisterer messagingRegisterer)
    {
        var enabled = messagingRegisterer.Configuration.GetValue<bool>($"{SectionName}:Enabled");

        if (!enabled)
        {
            return messagingRegisterer;
        }
        
        messagingRegisterer.Services.TryDecorate(typeof(IMessageHandler<>), typeof(IgnoreOutOfOrderMessageDecorator<>));
        messagingRegisterer.Services.AddSingleton(provider =>
        {
            var factory = (Type t) =>
            {
                var scope = provider.CreateScope();
                return scope.ServiceProvider.GetService(t);
            };
            var logger = provider.GetService<ILogger<OutOfOrderDetector>>();
            return new OutOfOrderDetector(factory, logger!);
        });
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        messagingRegisterer.Services.Scan(x => x.FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IGetMessageRelatedEntityVersion<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        return messagingRegisterer;
    }
}
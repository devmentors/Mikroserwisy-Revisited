using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Services.PersonalInfoVault.Core.Initializers;

public static class Extensions
{
    public static IServiceCollection AddDemoPersonalInfoInitializer(this IServiceCollection services)
    {
        services.AddHostedService<DemoPersonalInfoInitializer>();
        return services;
    }
}

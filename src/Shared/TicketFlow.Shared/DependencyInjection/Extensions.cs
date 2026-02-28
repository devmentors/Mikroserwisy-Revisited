using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.DependencyInjection;

internal static class Extensions
{
    public static IServiceCollection ReplaceWithSingletonService<TService, TImplementation>(this IServiceCollection services) 
        where TService : class where TImplementation : class, TService
    {
        var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TService));
        services.Remove(serviceDescriptor!);

        services.AddSingleton<TService, TImplementation>();
        return services;
    }
    
    public static IServiceCollection ReplaceWithTransientService<TService, TImplementation>(this IServiceCollection services) 
        where TService : class where TImplementation : class, TService
    {
        var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TService));
        services.Remove(serviceDescriptor!);

        services.AddTransient<TService, TImplementation>();
        return services;
    }
}
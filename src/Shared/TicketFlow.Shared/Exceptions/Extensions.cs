using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Exceptions;

public static class Extensions
{
    public static IServiceCollection AddExceptions(this IServiceCollection services)
    {
        services.AddSingleton<ExceptionMiddleware>();
        return services;
    }
    
    public static WebApplication UseExceptions(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }
}
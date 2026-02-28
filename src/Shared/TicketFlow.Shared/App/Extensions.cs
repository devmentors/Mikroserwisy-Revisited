using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.App;

public static class Extensions
{
    private const string SectionName = "App";
    
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration configuration, string sectionName = SectionName)
    {
        var section = configuration.GetSection(sectionName);
        services.Configure<AppOptions>(section);
        return services;
    }
}
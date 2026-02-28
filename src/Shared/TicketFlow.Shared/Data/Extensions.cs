using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Data;

public static class Extensions
{
    private const string SectionName = "Postgres";

    public static IServiceCollection AddPostgres<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration, 
        string sectionName = SectionName) where TDbContext : DbContext
    {
        var dbConnectionString = configuration.GetValue<string>($"{SectionName}:ConnectionString");
        
        services.AddDbContext<TDbContext>(x => x.UseNpgsql(dbConnectionString, 
            options => options.MigrationsAssembly(typeof(TDbContext).Assembly.GetName().Name)));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        return services;
    }
}
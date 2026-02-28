using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketFlow.CourseUtils;

namespace TicketFlow.Shared.Caching;

public static class Extensions
{
    private const string SectionName = "Cache";

    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SectionName)
    {
        var section = configuration.GetSection(sectionName);

        if (!section.Exists())
        {
            services.AddSingleton<ICacheService, NoopCacheService>();
            return services;
        }

        services.Configure<CacheOptions>(section);

        var options = new CacheOptions();
        section.Bind(options);

        if (FeatureFlags.UseRedisKeyPrefixes && string.IsNullOrWhiteSpace(options.InstanceName))
        {
            throw new InvalidOperationException(
                $"Cache.InstanceName is required when caching is enabled. " +
                $"Each service must have a unique prefix to prevent key collisions.");
        }

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            try
            {
                var connection = ConnectionMultiplexer.Connect(options.ConnectionString);
                logger.LogInformation(
                    "Connected to Redis at {ConnectionString} with instance name: {InstanceName}",
                    options.ConnectionString,
                    options.InstanceName);
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to connect to Redis at {ConnectionString}. Caching will be disabled.",
                    options.ConnectionString);
                throw;
            }
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddCachingWithFallback(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SectionName)
    {
        var section = configuration.GetSection(sectionName);

        if (!section.Exists())
        {
            services.AddSingleton<ICacheService, NoopCacheService>();
            return services;
        }

        services.Configure<CacheOptions>(section);

        var options = new CacheOptions();
        section.Bind(options);

        if (FeatureFlags.UseRedisKeyPrefixes && string.IsNullOrWhiteSpace(options.InstanceName))
        {
            services.AddSingleton<ICacheService, NoopCacheService>();
            return services;
        }

        services.AddSingleton<ICacheService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            try
            {
                var connection = ConnectionMultiplexer.Connect(options.ConnectionString);
                logger.LogInformation(
                    "Connected to Redis with instance name: {InstanceName}",
                    options.InstanceName);

                var cacheOptions = Microsoft.Extensions.Options.Options.Create(options);
                return new RedisCacheService(connection, cacheOptions, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable, using no-op cache");
                return new NoopCacheService();
            }
        });

        return services;
    }
}

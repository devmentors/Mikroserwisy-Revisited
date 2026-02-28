using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.AspNetCore;

public static class HttpApiExtensions
{
    private static readonly string CorsPolicyName = "CorsPolicy";

    public static IServiceCollection AddApiForFrontendConfigured(this IServiceCollection service)
    {
        return service.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, cors =>
                    cors.SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            })
            .Configure<JsonOptions>(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.SerializerOptions.Converters.Add(enumConverter);
                opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
    }

    public static void ExposeApiForFrontend(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);
    }
}
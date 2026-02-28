using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Agents.ChatBot.Configuration;
using TicketFlow.Agents.ChatBot.Services;
using TicketFlow.Agents.Shared;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.Agents.Shared.Metrics;
using TicketFlow.Agents.Shared.Services;

namespace TicketFlow.Agents.ChatBot;

public static class Extensions
{
    public static IServiceCollection AddChatBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChatBotPromptsOptions>(configuration.GetSection(ChatBotPromptsOptions.SectionName));

        services.Configure<ChatBotOptions>(configuration.GetSection("ChatBot"));

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:21200")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        services.AddAGUI();
        services.AddAgentSharedServices();
        services.AddLangfuse(configuration);
        services.AddSingleton<IPromptService, PromptService>();
        services.AddSingleton<IToolsLoader, ToolsLoader>();
        services.AddA2ASupport();
        services.AddLlmClient(configuration, "ChatBot");
        
        var metricsEnabled = configuration.GetValue<bool>("metrics:prometheus:enabled");
        if (metricsEnabled)
        {
            services.AddSingleton<AiMetrics>();
        }
        
        var serviceName = configuration["App:AppName"] ?? "agents-chatbot";
        var jaegerEndpoint = configuration["Jaeger:OtlpEndpoint"] ?? "http://localhost:4317";

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/metrics")
                        && !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddSource("Microsoft.Agents.AI.*")
                .AddSource("TicketFlow.Agents.*")
                .AddSource("TicketFlow.Agents.AI")
                .AddOtlpExporter(options => options.Endpoint = new Uri(jaegerEndpoint)));

        if (metricsEnabled)
        {
            otelBuilder.WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(AiMetrics.MeterName)
                .AddPrometheusExporter());
        }

        return services;
    }
}

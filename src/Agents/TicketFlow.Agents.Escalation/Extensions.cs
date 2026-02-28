using A2A;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Agents.Escalation.Agents;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Services;
using OpenTelemetry.Metrics;
using TicketFlow.Agents.Shared;
using TicketFlow.Agents.Shared.Metrics;
using TicketFlow.Agents.Shared.Services;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Observability;

namespace TicketFlow.Agents.Escalation;

public static class Extensions
{
    public static IServiceCollection AddEscalationAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddObservability(configuration);
        services.AddMetrics(configuration);

        services.Configure<EscalationOptions>(configuration.GetSection("Escalation"));
        var escalationOptions = configuration.GetSection("Escalation").Get<EscalationOptions>() ?? new();
        services.Configure<EscalationPromptsOptions>(configuration.GetSection(EscalationPromptsOptions.SectionName));

        var serviceName = configuration["App:AppName"] ?? "agents-escalation";
        
        services.AddLangfuse(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/metrics")
                            && !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("Microsoft.Agents.AI.*")
                    .AddSource("TicketFlow.Agents.AI")
                    .AddSource("TicketFlow.Agents.Escalation.*")
                    .AddSource("TicketFlow.Agents.Escalation.AutonomousRunner")
                    .AddOtlpExporter(options => options.Endpoint = new Uri(escalationOptions.JaegerOtlpEndpoint));
            });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:21000",
                        "http://localhost:21001",
                        "http://localhost:21002",
                        "http://localhost:21003")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        services.AddHttpContextAccessor();

        var taskManager = new TaskManager();
        services.AddSingleton<ITaskManager>(taskManager);
        services.AddSingleton(taskManager);

        services.AddAgentSharedServices();

        services.AddA2ASupport();

        services.AddSingleton<IPromptService, PromptService>();

        var metricsEnabled = configuration.GetValue<bool>("metrics:prometheus:enabled");
        if (metricsEnabled)
        {
            services.AddSingleton<AiMetrics>();
            services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(AiMetrics.MeterName)
                    .AddPrometheusExporter());
        }

        services.AddLlmClient(configuration, "Escalation");

        services.AddSingleton<IPlanningService, PlanningService>();
        services.AddSingleton<ILlmReflectionService, LlmReflectionService>();
        services.AddSingleton<IEscalationToolsLoader, EscalationToolsLoader>();
        services.AddSingleton<IAutonomousAgentRunner, AutonomousAgentRunner>();
        
        services.AddSingleton<EscalationConversationAgent>();

        return services;
    }
}

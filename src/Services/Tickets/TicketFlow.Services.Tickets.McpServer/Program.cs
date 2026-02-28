using ModelContextProtocol.Server;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Services.Tickets.McpServer;
using TicketFlow.Services.Tickets.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["App:AppName"] ?? "mcp-tickets";
var jaegerEndpoint = builder.Configuration["Jaeger:OtlpEndpoint"] ?? "http://localhost:4317";
var metricsEnabled = builder.Configuration.GetValue<bool>("metrics:prometheus:enabled");

var otelBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/metrics")
                && !context.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddSource("TicketFlow.Mcp.*")
        .AddOtlpExporter(options => options.Endpoint = new Uri(jaegerEndpoint)));

if (metricsEnabled)
{
    otelBuilder.WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<TicketsMcpServerOptions>(builder.Configuration.GetSection("McpServer"));

builder.Services.AddHttpClient<TicketsApiClient>(client =>
{
    var baseUrl = builder.Configuration["TicketsApi:BaseUrl"] ?? "http://localhost:5400";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<CommunicationApiClient>(client =>
{
    var baseUrl = builder.Configuration["McpServer:CommunicationApiBaseUrl"] ?? "http://localhost:5600";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Here goes our quasi auth :)
var toolRoleMap = new Dictionary<string, string[]>
{
    // Basic tools - all authenticated users
    ["list_tickets_good"] = ["agent", "supervisor", "admin", "escalation_agent"],
    ["get_ticket"] = ["agent", "supervisor", "admin", "escalation_agent"],
    ["get_client_notes"] = ["agent", "supervisor", "admin", "escalation_agent"],
    ["add_client_note"] = ["agent", "supervisor", "admin", "escalation_agent"],
    ["list_agents"] = ["agent", "supervisor", "admin", "escalation_agent"],

    // Privileged tools - escalation operations
    ["qualify_ticket"] = ["supervisor", "admin", "escalation_agent"],
    ["apply_qualification"] = ["supervisor", "admin", "escalation_agent"],
    ["assign_ticket"] = ["supervisor", "admin", "escalation_agent"],
    ["set_ticket_waiting"] = ["supervisor", "admin", "escalation_agent"],
    ["notify_supervisor"] = ["supervisor", "admin", "escalation_agent"],

    // Knowledge mining tools - for AI agents searching past solutions
    ["search_resolved_tickets"] = ["agent", "supervisor", "admin", "escalation_agent", "kb_agent"],
};

var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithHttpTransport();

mcpBuilder
    .WithTools<ListTicketsGoodTool>()
    .WithTools<GetTicketTool>()
    .WithTools<GetClientNotesTool>()
    .WithTools<AddClientNoteTool>()
    .WithTools<ListAgentsTool>()
    .WithTools<QualifyTicketTool>()
    .WithTools<AssignTicketTool>()
    .WithTools<SetTicketWaitingTool>()
    .WithTools<NotifySupervisorTool>()
    .WithTools<SearchResolvedTicketsTool>()
    .WithTools<ApplyQualificationTool>(); // You can also use assembly scanning - your choice :)

mcpBuilder.AddListToolsFilter(next => async (context, cancellationToken) =>
    {
        var result = await next(context, cancellationToken);

        var httpContext = context.Server.Services?.GetService<IHttpContextAccessor>()?.HttpContext;
        var role = httpContext?.Request.Headers["X-User-Role"].FirstOrDefault()?.ToLowerInvariant() ?? "";

        var filteredTools = result.Tools?
            .Where(t => toolRoleMap.TryGetValue(t.Name, out var allowedRoles) && allowedRoles.Contains(role))
            .ToList();

        return new ModelContextProtocol.Protocol.ListToolsResult
        {
            Tools = filteredTools ?? []
        };
    });

var app = builder.Build();
app.UseCors();

if (metricsEnabled)
{
    app.MapPrometheusScrapingEndpoint();
}

app.MapMcp();
app.MapGet("/mcp/card", () =>
{
    var card = new
    {
        name = "TicketFlow Tickets MCP Server",
        description = "MCP server providing ticket management operations for AI agents. Supports role-based tool filtering via X-User-Role header.",
        version = "1.0.0",
        protocol = "mcp",
        protocolVersion = "1.0",
        transport = new
        {
            type = "http",
            endpoint = "/mcp"
        },
        authentication = new
        {
            type = "header",
            headerName = "X-User-Role",
            description = "Role-based access control. Tools are filtered based on user role.",
            supportedRoles = new[] { "client", "agent", "supervisor", "admin", "escalation_agent", "kb_agent" }
        },
        tools = new[]
        {
            new { name = "list_tickets_good", description = "Returns semantic summary with actionable insights", roles = new[] { "agent", "supervisor", "admin", "escalation_agent" } },
            new { name = "get_ticket", description = "Retrieve detailed ticket information by ID", roles = new[] { "agent", "supervisor", "admin", "escalation_agent" } },
            new { name = "get_client_notes", description = "Get all notes/comments for a ticket", roles = new[] { "agent", "supervisor", "admin", "escalation_agent" } },
            new { name = "add_client_note", description = "Add a public note to a ticket", roles = new[] { "agent", "supervisor", "admin", "escalation_agent" } },
            new { name = "list_agents", description = "List available support agents with capacity info", roles = new[] { "agent", "supervisor", "admin", "escalation_agent" } },
            new { name = "qualify_ticket", description = "AI-powered ticket categorization and priority assignment", roles = new[] { "supervisor", "admin", "escalation_agent" } },
            new { name = "assign_ticket", description = "Assign ticket to an available agent (load-balanced)", roles = new[] { "supervisor", "admin", "escalation_agent" } },
            new { name = "set_ticket_waiting", description = "Put ticket in waiting queue when no agents available", roles = new[] { "supervisor", "admin", "escalation_agent" } },
            new { name = "notify_supervisor", description = "Send urgent notification to supervisor for critical tickets", roles = new[] { "supervisor", "admin", "escalation_agent" } },
            new { name = "search_resolved_tickets", description = "Search past resolved tickets for knowledge mining", roles = new[] { "agent", "supervisor", "admin", "escalation_agent", "kb_agent" } }
        },
        capabilities = new
        {
            roleBasedFiltering = true,
            semanticResponses = true,
            aiIntegration = true,
            observability = new
            {
                openTelemetry = true,
                prometheus = metricsEnabled
            }
        },
        documentation = new
        {
            backstageCatalog = "ServiceCatalog/catalog-info.yaml"
        }
    };
    return Results.Ok(card);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TicketFlow.Tickets.McpServer" }));

app.Run();

namespace TicketFlow.Services.Tickets.McpServer
{
    public partial class Program { }
}

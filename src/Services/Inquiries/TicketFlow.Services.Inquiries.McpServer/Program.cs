using ModelContextProtocol.Server;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Services.Inquiries.McpServer;
using TicketFlow.Services.Inquiries.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["App:AppName"] ?? "mcp-inquiries";
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

builder.Services.AddHttpClient<InquiriesApiClient>(client =>
{
    var baseUrl = builder.Configuration["InquiriesApi:BaseUrl"] ?? "http://localhost:5500";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var toolRoleMap = new Dictionary<string, string[]>
{
    ["list_my_inquiries"] = ["client"],
    ["check_inquiry_status"] = ["client"],
};

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ListMyInquiriesTool>()
    .WithTools<CheckInquiryStatusTool>()
    .AddListToolsFilter(next => async (context, cancellationToken) =>
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
        name = "TicketFlow Inquiries MCP Server",
        description = "MCP server providing client inquiry operations. Client-scoped access only - users can only see their own inquiries.",
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
            headers = new[]
            {
                new { name = "X-User-Role", description = "Role-based access control", required = true },
                new { name = "X-User-Id", description = "User GUID for scoping data access", required = true }
            },
            supportedRoles = new[] { "client" }
        },
        tools = new[]
        {
            new { name = "list_my_inquiries", description = "List inquiries belonging to the authenticated user", roles = new[] { "client" } },
            new { name = "check_inquiry_status", description = "Check the status of a specific inquiry", roles = new[] { "client" } }
        },
        capabilities = new
        {
            roleBasedFiltering = true,
            userScopedData = true,
            semanticResponses = true,
            observability = new
            {
                openTelemetry = true,
                prometheus = metricsEnabled
            }
        },
        securityNotes = new[]
        {
            "All data is scoped to the authenticated user (X-User-Id header)",
            "Only 'client' role can access these tools",
            "Compare with Inquiries.McpServer.AntiPattern for security best practices"
        },
        documentation = new
        {
            backstageCatalog = "ServiceCatalog/catalog-info.yaml",
            antiPattern = "See TicketFlow.Services.Inquiries.McpServer.AntiPattern for what NOT to do"
        }
    };
    return Results.Ok(card);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TicketFlow.Inquiries.McpServer" }));

app.Run();

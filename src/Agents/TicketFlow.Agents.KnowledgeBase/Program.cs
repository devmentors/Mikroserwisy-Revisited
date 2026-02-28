using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Agents.KnowledgeBase.Agent;
using TicketFlow.Agents.KnowledgeBase.Services;
using TicketFlow.Agents.Shared;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.Agents.Shared.Services;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAgentSharedServices();
builder.Services.AddLangfuse(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddMetrics(builder.Configuration);
builder.Services.AddLlmClient(builder.Configuration);

builder.Services.AddSingleton<IFaqSearchService, FaqSearchService>();
builder.Services.AddHttpClient<ITicketsMcpClient, TicketsMcpClient>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("X-User-Role", "kb_agent");
    });
builder.Services.AddScoped<KnowledgeBaseAgent>();

var serviceName = builder.Configuration["App:AppName"] ?? "agents-knowledgebase";
var jaegerEndpoint = builder.Configuration["Jaeger:OtlpEndpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/metrics")
                && !context.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddSource("TicketFlow.Agents.*")
        .AddSource("TicketFlow.Agents.KnowledgeBase")
        .AddOtlpExporter(options => options.Endpoint = new Uri(jaegerEndpoint)));

var app = builder.Build();
app.UseCors();
app.UseMetrics();

app.Use(async (context, next) =>
{
    var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
    var parentTraceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault();
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();

    if (!string.IsNullOrEmpty(sessionId) || !string.IsNullOrEmpty(parentTraceId))
    {
        app.Logger.LogDebug("A2A trace context received: sessionId={SessionId}, parentTraceId={ParentTraceId}, userId={UserId}",
            sessionId, parentTraceId, userId);
    }

    using (AmbientTraceContext.SetContext(new LangfuseTraceContext
    {
        SessionId = sessionId,
        ParentTraceId = parentTraceId,
        UserId = userId
    }))
    {
        await next();
    }
});

app.MapPost("/a2a", async (HttpContext context, KnowledgeBaseAgent agent) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    A2aRequest? request;
    try
    {
        request = JsonSerializer.Deserialize<A2aRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (JsonException ex)
    {
        app.Logger.LogError(ex, "Failed to parse A2A request");
        return Results.Json(new
        {
            jsonrpc = "2.0",
            id = (string?)null,
            error = new { code = -32700, message = "Parse error" }
        });
    }

    if (request?.Method != "message/send")
    {
        return Results.Json(new
        {
            jsonrpc = "2.0",
            id = request?.Id,
            error = new { code = -32601, message = $"Method not found: {request?.Method}" }
        });
    }

    var textPart = request.Params?.Message?.Parts?
        .FirstOrDefault(p => p.Kind == "text");

    if (textPart?.Text == null)
    {
        return Results.Json(new
        {
            jsonrpc = "2.0",
            id = request.Id,
            error = new { code = -32602, message = "No text in message" }
        });
    }

    app.Logger.LogInformation("[A2A] Received query from agent: {Query}", textPart.Text);

    var result = await agent.HandleQueryAsync(textPart.Text);

    app.Logger.LogInformation(
        "[A2A] KB Agent completed in {Steps} steps, source: {Source}",
        result.StepCount, result.Source);

    return Results.Json(new
    {
        jsonrpc = "2.0",
        id = request.Id,
        result = new
        {
            kind = "agent",
            messageId = $"kb-resp-{request.Id}",
            role = "agent",
            parts = new[]
            {
                new { kind = "text", text = result.Message }
            },
            metadata = new
            {
                source = result.Source,
                stepCount = result.StepCount,
                faqResultsCount = result.FaqResults.Count,
                ticketResultsCount = result.ResolvedTickets.Count
            }
        }
    });
});

app.MapGet("/.well-known/agent-card.json", GetKBAgentCard);

IResult GetKBAgentCard() => Results.Json(new
{
    name = "TicketFlow Knowledge Base Agent",
    description = "LLM-powered agent for knowledge base search. " +
                  "Uses AI to autonomously decide search strategy across FAQ and resolved tickets.",
    url = "http://localhost:8106",
    version = "2.0.0",
    defaultInputModes = new[] { "text" },
    defaultOutputModes = new[] { "text" },
    capabilities = new
    {
        streaming = true,
        pushNotifications = false
    },
    skills = new[]
    {
        new
        {
            id = "search-knowledge-base",
            name = "Search Knowledge Base",
            description = "Search FAQ and resolved tickets for answers to questions using LLM-driven strategy",
            tags = new[] { "knowledge-base", "search", "faq", "llm" },
            inputModes = new[] { "text" },
            outputModes = new[] { "text" },
            examples = new[]
            {
                "How do I reset my password?",
                "What is the refund policy?"
            }
        }
    }
});

app.MapPost("/", async (HttpContext context, KnowledgeBaseAgent agent,
    IAguiRequestHandler aguiHandler, Microsoft.Extensions.AI.IChatClient chatClient) =>
{
    context.Request.EnableBuffering();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

    app.Logger.LogInformation("[AG-UI] Received KB request");

    await aguiHandler.HandleRequestAsync(
        context,
        chatClient,
        agent.Tools,
        agent.SystemPrompt,
        requestBody,
        new AguiHandlerOptions { MaxIterationsPerRequest = 5 });
});

app.MapGet("/health", (KnowledgeBaseAgent agent) => Results.Ok(new
{
    status = "healthy",
    agent = "knowledge-base",
    framework = "C#",
    llmPowered = true,
    tools = agent.Tools.Select(t => t.Name).ToArray(),
    maxIterations = 5
}));

app.Run();

record A2aRequest(string Jsonrpc, string Id, string Method, A2aParams? Params);
record A2aParams(A2aMessage? Message);
record A2aMessage(string Kind, string MessageId, string Role, List<A2aPart>? Parts);
record A2aPart(string Kind, string? Text);

public partial class Program { }

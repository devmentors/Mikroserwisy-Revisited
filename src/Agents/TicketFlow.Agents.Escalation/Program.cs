using A2A;
using A2A.AspNetCore;
using Microsoft.Extensions.AI;
using TicketFlow.Agents.Escalation;
using TicketFlow.Agents.Escalation.Agents;
using TicketFlow.Agents.Escalation.Services;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.Agents.Shared.Services;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("prompts.json", optional: false, reloadOnChange: true);
builder.Services.AddEscalationAgent(builder.Configuration);

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

var taskManager = app.Services.GetRequiredService<ITaskManager>();
var escalationAgent = app.Services.GetRequiredService<EscalationConversationAgent>();
escalationAgent.Attach(taskManager);

app.MapPost("/", async (
    HttpContext context,
    IUserContextExtractor extractor,
    IAguiRequestHandler aguiHandler,
    IChatClient chatClient,
    IEscalationToolsLoader toolsLoader,
    IPromptService promptService) =>
{
    string? handoverContext = null;
    var handoverContextHeader = context.Request.Headers["X-Handover-Context"].FirstOrDefault();

    if (!string.IsNullOrEmpty(handoverContextHeader))
    {
        try
        {
            var decodedJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(handoverContextHeader));
            var handoverData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(decodedJson);
            var skill = handoverData.GetProperty("skill").GetString();
            var args = handoverData.GetProperty("args").ToString();

            handoverContext = $"This user was transferred from another agent via the '{skill}' skill.\n" +
                             $"Context data: {args}\n" +
                             $"IMPORTANT: Extract the ticketId from the context and use it throughout this conversation. " +
                             $"Do NOT ask the user for the ticketId again - it's already provided in the context.";

        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to parse handover context header");
        }
    }

    context.Request.EnableBuffering();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

    var tools = await toolsLoader.LoadToolsAsync();
    var toolsArray = tools.ToArray();
    var systemPrompt = promptService.GetInstructions(toolsArray, handoverContext);

    await aguiHandler.HandleRequestAsync(
        context,
        chatClient,
        toolsArray,
        systemPrompt,
        requestBody);
});

app.MapA2A(taskManager, "/a2a");
app.MapWellKnownAgentCard(taskManager, "/a2a");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", agent = "escalation" }));

app.Run();

public partial class Program { }

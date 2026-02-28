using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.ChatBot;
using TicketFlow.Agents.ChatBot.Configuration;
using TicketFlow.Agents.ChatBot.Services;
using TicketFlow.Agents.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("prompts.json", optional: false, reloadOnChange: true);
builder.Services.AddChatBot(builder.Configuration);

var app = builder.Build();
app.UseCors();

if (builder.Configuration.GetValue<bool>("metrics:prometheus:enabled"))
{
    app.MapPrometheusScrapingEndpoint();
}

app.MapGet("/tools", async (
    HttpContext context,
    IUserContextExtractor extractor,
    IToolsLoader toolsLoader) =>
{
    var userContext = extractor.Extract(context);
    app.Logger.LogInformation("Tools request from {Email} with role {Role}", userContext.Email, userContext.Role);

    var tools = await toolsLoader.GetAvailableToolsInfoAsync(userContext, context.RequestAborted);

    return Results.Ok(new
    {
        role = userContext.Role,
        email = userContext.Email,
        agentId = userContext.AgentId,
        tools = tools.Select(t => new { name = t.Name, description = t.Description, server = t.Server })
    });
});

app.MapPost("/", async (
    HttpContext context,
    IUserContextExtractor extractor,
    IToolsLoader toolsLoader,
    IPromptService promptService,
    IAguiRequestHandler aguiHandler,
    IChatClient chatClient) =>
{
    var userContext = extractor.Extract(context);
    app.Logger.LogInformation("Chat request from {Email} with role {Role}", userContext.Email, userContext.Role);

    await using var result = await toolsLoader.LoadToolsAsync(userContext, context.RequestAborted);

    context.Request.EnableBuffering();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

    var systemPrompt = promptService.GetInstructions(userContext, result.Tools.ToArray());

    await aguiHandler.HandleRequestAsync(
        context,
        chatClient,
        result.Tools.ToArray(),
        systemPrompt,
        requestBody);
});

app.MapGet("/agents", (IOptions<ChatBotOptions> options) =>
{
    var a2aAgents = options.Value.A2AAgents
        .Select(a => new { name = a.Key, url = a.Value, protocol = "a2a" })
        .ToList();

    return Results.Ok(new
    {
        a2aAgents
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TicketFlow.ChatBot.Agent" }));

app.Run();

public partial class Program { }

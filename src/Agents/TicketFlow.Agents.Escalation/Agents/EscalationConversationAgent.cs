using A2A;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Services;

namespace TicketFlow.Agents.Escalation.Agents;

public class EscalationConversationAgent
{
    private readonly IAutonomousAgentRunner _agentRunner;
    private readonly IEscalationToolsLoader _toolsLoader;
    private readonly EscalationOptions _options;
    private readonly ILogger<EscalationConversationAgent> _logger;
    private ITaskManager _taskManager = null!;

    public EscalationConversationAgent(
        IAutonomousAgentRunner agentRunner,
        IEscalationToolsLoader toolsLoader,
        IOptions<EscalationOptions> options,
        ILogger<EscalationConversationAgent> logger)
    {
        _agentRunner = agentRunner;
        _toolsLoader = toolsLoader;
        _options = options.Value;
        _logger = logger;
    }

    public void Attach(ITaskManager taskManager)
    {
        _taskManager = taskManager;

        // No OnMessageReceived — the library auto-creates a task and routes to OnTaskCreated.
        // This enables streaming: the library sets up SSE before calling OnTaskCreated,
        // so UpdateStatusAsync calls inside the handler push events to the stream.

        taskManager.OnTaskCreated = async (task, ct) => await ProcessTaskAsync(task, ct);

        taskManager.OnAgentCardQuery = (agentUrl, ct) => Task.FromResult(new A2A.AgentCard
        {
            Name = "TicketFlow Escalation Agent",
            Description = "Autonomous escalation specialist - handles ticket assignment, capacity issues, and supervisor notifications using multi-step reasoning with reflection",
            Url = agentUrl,
            Version = "3.0.0",
            Capabilities = new A2A.AgentCapabilities
            {
                Streaming = true,
                PushNotifications = false
            },
            Skills =
            [
                new A2A.AgentSkill
                {
                    Id = "escalate-ticket",
                    Name = "Escalate Ticket",
                    Description = "Autonomously handle ticket escalation with multi-step reasoning. Attempts assignment, reflects on failures, and tries alternative strategies including supervisor notification.",
                    Tags = ["escalation", "tickets", "autonomous"],
                    InputModes = ["text"],
                    OutputModes = ["text"],
                    Examples =
                    [
                        "Escalate ticket dc63e171-79f3-4b53-9d18-a91b110cba06 - urgent issue",
                        "Please help with ticket abc-123, no agents available"
                    ]
                }
            ]
        });
    }

    private async Task ProcessTaskAsync(AgentTask task, CancellationToken ct)
    {
        try
        {
            var inputText = task.History?.LastOrDefault(m => m.Role == MessageRole.User) is { } lastUserMsg
                ? ExtractTextFromMessage(lastUserMsg)
                : null;

            if (string.IsNullOrEmpty(inputText))
            {
                await _taskManager.UpdateStatusAsync(task.Id, TaskState.Failed,
                    message: CreateTextResponse("Nie otrzymałem żadnej wiadomości."), final: true, cancellationToken: ct);
                return;
            }

            await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working,
                message: CreateTextResponse("Starting escalation..."), final: false, cancellationToken: ct);

            var response = await HandleConversationAsync(inputText, ct, onStepProgress: async stepText =>
            {
                await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working,
                    message: CreateTextResponse(stepText), final: false, cancellationToken: ct);
            });

            await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed,
                message: CreateTextResponse(response), final: true, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing A2A streaming task {TaskId}", task.Id);
            await _taskManager.UpdateStatusAsync(task.Id, TaskState.Failed,
                message: CreateTextResponse($"Wystąpił błąd: {ex.Message}"), final: true, cancellationToken: ct);
        }
    }

    private static string? ExtractTextFromMessage(AgentMessage message)
    {
        if (message.Parts == null) return null;

        foreach (var part in message.Parts)
        {
            if (part is TextPart textPart && !string.IsNullOrEmpty(textPart.Text))
            {
                return textPart.Text;
            }
        }

        return null;
    }

    private async Task<string> HandleConversationAsync(
        string userMessage,
        CancellationToken ct,
        Func<string, Task>? onStepProgress = null)
    {
        var ticketId = ExtractTicketId(userMessage);
        if (ticketId == null)
        {
            return "Proszę podać ID ticketu, abym mógł pomóc. Przykład: 'Proszę o eskalację ticketu abc-123' lub po prostu podaj GUID.";
        }

        try
        {
            var tools = await _toolsLoader.LoadToolsAsync(ct);

            var result = await _agentRunner.RunAsync(
                goal: userMessage,
                tools: tools.ToArray(),
                maxSteps: _options.MaxSteps,
                ct: ct,
                onStepProgress: onStepProgress);

            return result.FinalMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in autonomous agent execution");
            return $"Wystąpił błąd podczas przetwarzania żądania: {ex.Message}";
        }
    }

    private Guid? ExtractTicketId(string message)
    {
        var words = message.Split([' ', '\n', '\r', '\t', '#'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var cleaned = word.Trim('.', ',', '!', '?', ';', ':', ')', ']', '}');

            if (Guid.TryParse(cleaned, out var guid))
            {
                return guid;
            }
        }

        return null;
    }

    private AgentMessage CreateTextResponse(string text)
    {
        return new AgentMessage
        {
            Role = MessageRole.Agent,
            Parts = [new TextPart { Text = text }]
        };
    }

}

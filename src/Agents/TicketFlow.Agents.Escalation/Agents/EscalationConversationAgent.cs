using A2A;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Models;
using TicketFlow.Agents.Escalation.Services;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Escalation.Agents;

public class EscalationConversationAgent
{
    private readonly IAutonomousAgentRunner _agentRunner;
    private readonly IEscalationToolsLoader _toolsLoader;
    private readonly EscalationOptions _options;
    private readonly ILogger<EscalationConversationAgent> _logger;

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
        taskManager.OnMessageReceived = async (msg, ct) => await ProcessMessageAsync(msg, ct);

        taskManager.OnAgentCardQuery = (agentUrl, ct) => Task.FromResult(new A2A.AgentCard
        {
            Name = "TicketFlow Escalation Agent",
            Description = "Autonomous escalation specialist - handles ticket assignment, capacity issues, and supervisor notifications using multi-step reasoning with reflection",
            Url = agentUrl,
            Version = "3.0.0",
            Capabilities = new A2A.AgentCapabilities
            {
                Streaming = false,
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

    private async Task<AgentMessage> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken ct)
    {
        try
        {
            var message = messageSendParams.Message;
            string? inputText = null;

            if (message.Parts != null)
            {
                foreach (var part in message.Parts)
                {
                    if (part is TextPart textPart && !string.IsNullOrEmpty(textPart.Text))
                    {
                        inputText = textPart.Text;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(inputText))
            {
                return CreateTextResponse("Nie otrzymałem żadnej wiadomości. Proszę spróbować ponownie.");
            }
            
            var userContext = UseAgentUserCtx();
            var response = await HandleConversationAsync(inputText, userContext, ct);
            return CreateTextResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing A2A message");
            return CreateTextResponse($"Wystąpił błąd: {ex.Message}. Proszę spróbować ponownie.");
        }
    }
    
    private async Task<string> HandleConversationAsync(
        string userMessage,
        UserContext userContext,
        CancellationToken ct)
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
                ct: ct);

            return result.FinalMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in autonomous agent execution");
            return $"Wystąpił błąd podczas przetwarzania żądania: {ex.Message}";
        }
    }

    private UserContext UseAgentUserCtx()
    {
        return new UserContext
        {
            Role = "escalation_agent",
            Email = "escalation@ticketflow.local"
        };
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

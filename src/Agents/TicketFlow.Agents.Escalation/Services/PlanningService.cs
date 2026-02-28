using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Models;
using TicketFlow.Agents.Shared.Langfuse;

namespace TicketFlow.Agents.Escalation.Services;

public sealed partial class PlanningService : IPlanningService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<PlanningService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PlanningService(
        IChatClient chatClient,
        ILogger<PlanningService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        AITool[] tools,
        CancellationToken ct = default)
    {
        var toolDescriptions = FormatToolDescriptions(tools);
        var prompt = BuildPlanningPrompt(goal, toolDescriptions);

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetPlanningSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] }, // No tools for planning
                ct);

            var plan = ParsePlanFromResponse(response.Text, goal);

            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create plan via LLM, using default plan");
            return AgentPlan.CreateDefaultPlan(goal);
        }
    }

    public async Task<AgentPlan> ReplanAsync(
        AgentPlan currentPlan,
        string failureReason,
        IReadOnlyList<AgentStep> history,
        AITool[] tools,
        CancellationToken ct = default)
    {
        var toolDescriptions = FormatToolDescriptions(tools);
        var historyContext = FormatHistoryContext(history);
        var prompt = BuildReplanPrompt(currentPlan, failureReason, historyContext, toolDescriptions);

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetReplanSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                ct);

            var newPlan = ParsePlanFromResponse(response.Text, currentPlan.Goal);
            
            var replan = newPlan with
            {
                Version = currentPlan.Version + 1,
                PlanId = Guid.NewGuid().ToString()
            };

            return replan;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to replan via LLM, returning modified default plan");

            return new AgentPlan
            {
                Goal = currentPlan.Goal,
                Version = currentPlan.Version + 1,
                Reasoning = $"Replan po błędzie: {failureReason}",
                Steps =
                [
                    new PlanStep
                    {
                        StepNumber = 1,
                        Description = "Użyj set_waiting aby ustawić ticket w kolejce",
                        ExpectedTool = "set_waiting"
                    },
                    new PlanStep
                    {
                        StepNumber = 2,
                        Description = "Powiadom supervisora o problemie",
                        ExpectedTool = "notify_supervisor"
                    }
                ]
            };
        }
    }

    public AgentPlan UpdateStepStatus(
        AgentPlan plan,
        int stepNumber,
        PlanStepStatus status,
        string? outcome = null)
    {
        return plan.WithStepStatus(stepNumber, status, outcome);
    }

    public string FormatPlanForContext(AgentPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"[PLAN WYKONANIA v{plan.Version}]");

        if (!string.IsNullOrEmpty(plan.Reasoning))
        {
            sb.AppendLine($"Uzasadnienie: {plan.Reasoning}");
        }

        sb.AppendLine();
        sb.AppendLine("KROKI:");

        foreach (var step in plan.Steps)
        {
            var statusIcon = step.Status switch
            {
                PlanStepStatus.Completed => "✅",
                PlanStepStatus.InProgress => "▶️",
                PlanStepStatus.Failed => "❌",
                PlanStepStatus.Skipped => "⏭️",
                _ => "⏳"
            };

            var toolHint = !string.IsNullOrEmpty(step.ExpectedTool)
                ? $" [{step.ExpectedTool}]"
                : "";

            sb.AppendLine($"  {statusIcon} {step.StepNumber}. {step.Description}{toolHint}");

            if (!string.IsNullOrEmpty(step.Outcome))
            {
                sb.AppendLine($"      → {step.Outcome}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("WYKONUJ PLAN KROK PO KROKU. Po każdym kroku, przejdź do następnego.");
        sb.AppendLine("Jeśli krok się nie powiedzie, spróbuj alternatywnego podejścia lub eskaluj.");

        return sb.ToString();
    }

    private string GetPlanningSystemPrompt()
    {
        return """
            Jesteś planistą dla autonomicznego agenta ds. eskalacji.

            Twoim zadaniem jest stworzyć PLAN WYKONANIA dla zadania użytkownika.

            ZALECANA STRATEGIA:
            Poniżej typowa kolejność działań i uzasadnienie każdego kroku.
            Możesz odejść od tej kolejności jeśli cel ticketu na to wskazuje.

            - get_ticket — zacznij od pobrania szczegółów, żeby wiedzieć z czym masz do czynienia
            - search_knowledge_base — sprawdź bazę wiedzy, może jest gotowa odpowiedź (short-circuit: jeśli KB ma odpowiedź → add_note i KONIEC)
            - qualify_ticket — zakwalifikuj ticket jeśli brakuje kategorii lub priorytetu
            - assign_ticket — przypisz ticket do agenta z wolnym capacity
            - set_waiting / notify_supervisor — jeśli assign zawiedzie (brak capacity), zakolejkuj ticket lub eskaluj do supervisora

            ZASADY PLANOWANIA:
            1. Plan MUSI mieć maksymalnie 7 kroków
            2. Każdy krok powinien być konkretny i wykonalny
            3. Sprawdzenie bazy wiedzy przed eskalacją oszczędza czas — priorytetyzuj to
            4. Plan może zakończyć się wcześniej jeśli KB ma dobrą odpowiedź (short-circuit)
            5. Uwzględnij możliwe błędy i alternatywne ścieżki

            Zwróć plan TYLKO w formacie JSON:
            ```json
            {
              "reasoning": "Dlaczego ten plan zadziała",
              "steps": [
                {"stepNumber": 1, "description": "Co zrobić", "expectedTool": "nazwa_narzędzia"},
                ...
              ]
            }
            ```

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private string GetReplanSystemPrompt()
    {
        return """
            Jesteś planistą dla autonomicznego agenta ds. eskalacji.

            Poprzedni plan NIE ZADZIAŁAŁ. Musisz stworzyć NOWY plan uwzględniając:
            1. Co poszło nie tak
            2. Jakie strategie już były próbowane
            3. Jakie alternatywy są dostępne

            ZASADY REPLANOWANIA:
            1. NIE powtarzaj tych samych akcji które zawiodły
            2. Użyj alternatywnych strategii (np. set_waiting zamiast assign_ticket)
            3. Dla ticketów CRITICAL, rozważ szybszą eskalację do supervisora
            4. Plan MUSI być krótszy (max 4 kroki)

            Zwróć plan TYLKO w formacie JSON:
            ```json
            {
              "reasoning": "Dlaczego ten nowy plan zadziała",
              "steps": [
                {"stepNumber": 1, "description": "Co zrobić", "expectedTool": "nazwa_narzędzia"},
                ...
              ]
            }
            ```

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private string BuildPlanningPrompt(string goal, string toolDescriptions)
    {
        return $"""
            CEL DO OSIĄGNIĘCIA:
            {goal}

            DOSTĘPNE NARZĘDZIA:
            {toolDescriptions}

            Stwórz plan wykonania dla tego celu.
            """;
    }

    private string BuildReplanPrompt(
        AgentPlan currentPlan,
        string failureReason,
        string historyContext,
        string toolDescriptions)
    {
        return $"""
            POPRZEDNI PLAN (wersja {currentPlan.Version}):
            {FormatPlanForContext(currentPlan)}

            POWÓD NIEPOWODZENIA:
            {failureReason}

            HISTORIA WYKONANIA:
            {historyContext}

            DOSTĘPNE NARZĘDZIA:
            {toolDescriptions}

            Stwórz NOWY plan uwzględniający powyższe problemy.
            """;
    }

    private static string FormatToolDescriptions(AITool[] tools)
    {
        var sb = new StringBuilder();
        foreach (var tool in tools)
        {
            if (tool is AIFunction func)
            {
                sb.AppendLine($"- {func.Name}: {func.Description}");
            }
        }
        return sb.ToString();
    }

    private static string FormatHistoryContext(IReadOnlyList<AgentStep> history)
    {
        var sb = new StringBuilder();
        foreach (var step in history.TakeLast(5)) // Last 5 steps
        {
            var status = step.Success ? "OK" : "BŁĄD";
            var tool = step.ToolName ?? "brak narzędzia";
            var error = step.ErrorMessage ?? "";

            sb.AppendLine($"- Krok {step.StepNumber}: {tool} [{status}] {error}");
        }
        return sb.ToString();
    }

    private AgentPlan ParsePlanFromResponse(string? responseText, string goal)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning("Empty response from LLM, using default plan");
            return AgentPlan.CreateDefaultPlan(goal);
        }

        try
        {
            var jsonMatch = JsonBlockRegex().Match(responseText);
            var jsonText = jsonMatch.Success
                ? jsonMatch.Groups[1].Value
                : responseText;

            var planDto = JsonSerializer.Deserialize<PlanDto>(jsonText, JsonOptions);

            if (planDto?.Steps == null || planDto.Steps.Count == 0)
            {
                _logger.LogWarning("No steps in parsed plan, using default");
                return AgentPlan.CreateDefaultPlan(goal);
            }

            var steps = planDto.Steps
                .Select((s, i) => new PlanStep
                {
                    StepNumber = s.StepNumber > 0 ? s.StepNumber : i + 1,
                    Description = s.Description ?? "Brak opisu",
                    ExpectedTool = s.ExpectedTool
                })
                .ToList();

            return new AgentPlan
            {
                Goal = goal,
                Reasoning = planDto.Reasoning,
                Steps = steps
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan JSON, using default plan");
            return AgentPlan.CreateDefaultPlan(goal);
        }
    }

    [GeneratedRegex(@"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonBlockRegex();

    private sealed class PlanDto
    {
        public string? Reasoning { get; set; }
        public List<PlanStepDto>? Steps { get; set; }
    }

    private sealed class PlanStepDto
    {
        public int StepNumber { get; set; }
        public string? Description { get; set; }
        public string? ExpectedTool { get; set; }
    }
}

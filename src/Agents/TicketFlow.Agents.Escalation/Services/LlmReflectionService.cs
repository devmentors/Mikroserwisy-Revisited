using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Escalation.Models;

namespace TicketFlow.Agents.Escalation.Services;

public sealed partial class LlmReflectionService : ILlmReflectionService
{
    private readonly IChatClient _chatClient;
    private readonly EscalationOptions _options;
    private readonly ILogger<LlmReflectionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LlmReflectionService(
        IChatClient chatClient,
        IOptions<EscalationOptions> options,
        ILogger<LlmReflectionService> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReflectionResult> ReflectOnFailureAsync(
        AgentStep failedStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default)
    {
        if (!_options.UseLlmReflection)
        {
            return ReflectionResult.CreateDefault(failedStep.ErrorMessage ?? "Nieznany błąd");
        }

        try
        {
            var prompt = BuildReflectionPrompt(failedStep, plan, history);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetReflectionSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                ct);

            return ParseReflectionResponse(response.Text, failedStep);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM reflection failed, using default");
            return ReflectionResult.CreateDefault(failedStep.ErrorMessage ?? "Nieznany błąd");
        }
    }

    public async Task<GoalCheckResult> IsGoalAchievedAsync(
        string? responseText,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default)
    {
        if (!_options.UseLlmReflection || string.IsNullOrWhiteSpace(responseText))
        {
            var hasSuccessMarker = responseText?.Contains("✅") == true;
            return new GoalCheckResult
            {
                IsAchieved = hasSuccessMarker,
                Reason = hasSuccessMarker ? "Wykryto marker sukcesu" : "Brak markera sukcesu",
                Confidence = 0.5
            };
        }

        try
        {
            var prompt = BuildGoalCheckPrompt(responseText, plan);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetGoalCheckSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                ct);

            return ParseGoalCheckResponse(response.Text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM goal check failed, using default");
            var hasSuccessMarker = responseText?.Contains("✅") == true;
            return new GoalCheckResult
            {
                IsAchieved = hasSuccessMarker,
                Reason = "Fallback - sprawdzenie markera sukcesu",
                Confidence = 0.3
            };
        }
    }

    public async Task<string> GenerateThoughtAsync(
        PlanStep? currentPlanStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default)
    {
        if (!_options.UseLlmReflection)
        {
            return currentPlanStep != null
                ? $"Muszę wykonać: {currentPlanStep.Description}"
                : "Analizuję sytuację i wybieram następną akcję...";
        }

        try
        {
            var prompt = BuildThoughtPrompt(currentPlanStep, plan, history);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetThoughtSystemPrompt()),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                ct);

            var thought = response.Text?.Trim() ?? "Kontynuuję wykonanie planu...";

            if (thought.Length > 500)
            {
                thought = thought[..497] + "...";
            }

            return thought;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thought via LLM");
            return currentPlanStep != null
                ? $"Muszę wykonać: {currentPlanStep.Description}"
                : "Kontynuuję wykonanie planu...";
        }
    }

    private string GetReflectionSystemPrompt()
    {
        return """
            Jesteś analitykiem błędów dla autonomicznego agenta ds. eskalacji.

            Twoim zadaniem jest przeanalizować DLACZEGO akcja się nie powiodła i zasugerować alternatywy.

            ZWRÓĆ ODPOWIEDŹ TYLKO W FORMACIE JSON:
            ```json
            {
              "analysis": "Krótka analiza dlaczego to nie zadziałało",
              "alternativeStrategies": ["Strategia 1", "Strategia 2"],
              "shouldReplan": true/false,
              "shouldEscalateToSupervisor": true/false,
              "suggestedNextAction": "Co zrobić dalej"
            }
            ```

            ZASADY:
            - shouldReplan=true gdy potrzebne jest zupełnie inne podejście
            - shouldEscalateToSupervisor=true gdy ticket jest CRITICAL i wielokrotne próby zawiodły
            - Sugeruj konkretne narzędzia do użycia

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private string GetGoalCheckSystemPrompt()
    {
        return """
            Jesteś weryfikatorem osiągnięcia celu dla agenta ds. eskalacji.

            Przeanalizuj czy CEL został osiągnięty na podstawie odpowiedzi agenta.

            ZWRÓĆ ODPOWIEDŹ TYLKO W FORMACIE JSON:
            ```json
            {
              "achieved": true/false,
              "reason": "Dlaczego tak uważasz",
              "remainingWork": "Co pozostało do zrobienia (jeśli achieved=false)"
            }
            ```

            Cel jest OSIĄGNIĘTY gdy:
            - Ticket został przypisany do agenta
            - LUB ticket został dodany do kolejki (set_waiting)
            - LUB supervisor został powiadomiony o problemie
            - LUB agent wyraźnie stwierdził zakończenie zadania (✅)

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private string GetThoughtSystemPrompt()
    {
        return """
            Jesteś autonomicznym agentem ds. eskalacji. Werbalizujesz swój proces myślowy.

            Na podstawie kontekstu, napisz KRÓTKĄ myśl (1-2 zdania) o tym:
            - Co planujesz zrobić
            - Dlaczego wybrałeś tę akcję
            - Co sprawdzisz w wyniku

            ZASADY:
            - MAX 2 zdania
            - Pisz w pierwszej osobie
            - Bądź konkretny, nie ogólnikowy

            ODPOWIADAJ TYLKO PO POLSKU!
            """;
    }

    private string BuildReflectionPrompt(
        AgentStep failedStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NIEPOWODZENIE:");
        sb.AppendLine($"- Krok: {failedStep.StepNumber}");
        sb.AppendLine($"- Narzędzie: {failedStep.ToolName ?? "brak"}");
        sb.AppendLine($"- Błąd: {failedStep.ErrorMessage ?? failedStep.ToolResult ?? "nieznany"}");
        sb.AppendLine();

        sb.AppendLine("AKTUALNY PLAN:");
        sb.AppendLine(FormatPlanSummary(plan));
        sb.AppendLine();

        sb.AppendLine("HISTORIA (ostatnie 5 kroków):");
        foreach (var step in history.TakeLast(5))
        {
            var status = step.Success ? "OK" : "BŁĄD";
            sb.AppendLine($"  {step.StepNumber}. {step.ToolName ?? "brak"} [{status}]");
        }

        sb.AppendLine();
        sb.AppendLine("Przeanalizuj problem i zasugeruj rozwiązanie.");

        return sb.ToString();
    }

    private string BuildGoalCheckPrompt(
        string responseText,
        AgentPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CEL: {plan.Goal}");
        sb.AppendLine();
        sb.AppendLine("PLAN:");
        sb.AppendLine(FormatPlanSummary(plan));
        sb.AppendLine();
        sb.AppendLine("OSTATNIA ODPOWIEDŹ AGENTA:");
        sb.AppendLine(responseText);
        sb.AppendLine();
        sb.AppendLine("Czy cel został osiągnięty?");

        return sb.ToString();
    }

    private string BuildThoughtPrompt(
        PlanStep? currentPlanStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CEL: {plan.Goal}");
        sb.AppendLine();

        if (currentPlanStep != null)
        {
            sb.AppendLine($"AKTUALNY KROK PLANU: {currentPlanStep.StepNumber}. {currentPlanStep.Description}");
            if (!string.IsNullOrEmpty(currentPlanStep.ExpectedTool))
            {
                sb.AppendLine($"SUGEROWANE NARZĘDZIE: {currentPlanStep.ExpectedTool}");
            }
        }
        else
        {
            sb.AppendLine("BRAK ZAPLANOWANEGO KROKU - wybierz najlepszą akcję");
        }

        if (history.Count > 0)
        {
            var lastStep = history.Last();
            sb.AppendLine();
            sb.AppendLine($"OSTATNI WYNIK: {lastStep.ToolName ?? "brak"} - {(lastStep.Success ? "OK" : "BŁĄD")}");
            if (!string.IsNullOrEmpty(lastStep.ToolResult))
            {
                var preview = lastStep.ToolResult.Length > 100
                    ? lastStep.ToolResult[..100] + "..."
                    : lastStep.ToolResult;
                sb.AppendLine($"WYNIK: {preview}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Co myślisz i co zrobisz?");

        return sb.ToString();
    }

    private static string FormatPlanSummary(AgentPlan plan)
    {
        var sb = new StringBuilder();
        foreach (var step in plan.Steps)
        {
            var statusIcon = step.Status switch
            {
                PlanStepStatus.Completed => "✅",
                PlanStepStatus.Failed => "❌",
                PlanStepStatus.InProgress => "▶️",
                _ => "⏳"
            };
            sb.AppendLine($"  {statusIcon} {step.StepNumber}. {step.Description}");
        }
        return sb.ToString();
    }

    private ReflectionResult ParseReflectionResponse(string? responseText, AgentStep failedStep)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return ReflectionResult.CreateDefault(failedStep.ErrorMessage ?? "Nieznany błąd");
        }

        try
        {
            var jsonMatch = JsonBlockRegex().Match(responseText);
            var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;

            var dto = JsonSerializer.Deserialize<ReflectionDto>(jsonText, JsonOptions);

            if (dto == null)
            {
                return ReflectionResult.CreateDefault(failedStep.ErrorMessage ?? "Nieznany błąd");
            }

            return new ReflectionResult
            {
                Analysis = dto.Analysis ?? "Brak analizy",
                AlternativeStrategies = dto.AlternativeStrategies ?? [],
                ShouldReplan = dto.ShouldReplan,
                ShouldEscalateToSupervisor = dto.ShouldEscalateToSupervisor,
                SuggestedNextAction = dto.SuggestedNextAction,
                Confidence = 0.7
            };
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to parse reflection JSON");
            return ReflectionResult.CreateDefault(failedStep.ErrorMessage ?? "Nieznany błąd");
        }
    }

    private GoalCheckResult ParseGoalCheckResponse(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new GoalCheckResult { IsAchieved = false, Reason = "Brak odpowiedzi", Confidence = 0.0 };
        }

        try
        {
            var jsonMatch = JsonBlockRegex().Match(responseText);
            var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;

            var dto = JsonSerializer.Deserialize<GoalCheckDto>(jsonText, JsonOptions);

            return new GoalCheckResult
            {
                IsAchieved = dto?.Achieved ?? false,
                Reason = dto?.Reason ?? "Brak uzasadnienia",
                RemainingWork = dto?.RemainingWork,
                Confidence = 0.8
            };
        }
        catch (JsonException)
        {
            return new GoalCheckResult
            {
                IsAchieved = false,
                Reason = "Nie udało się sparsować odpowiedzi",
                Confidence = 0.3
            };
        }
    }

    [GeneratedRegex(@"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonBlockRegex();

    private sealed class ReflectionDto
    {
        public string? Analysis { get; set; }
        public List<string>? AlternativeStrategies { get; set; }
        public bool ShouldReplan { get; set; }
        public bool ShouldEscalateToSupervisor { get; set; }
        public string? SuggestedNextAction { get; set; }
    }

    private sealed class GoalCheckDto
    {
        public bool Achieved { get; set; }
        public string? Reason { get; set; }
        public string? RemainingWork { get; set; }
    }
}

namespace TicketFlow.Agents.Escalation.Models;

public sealed record AgentPlan
{
    public string PlanId { get; init; } = Guid.NewGuid().ToString();
    public string Goal { get; init; } = string.Empty;
    public List<PlanStep> Steps { get; init; } = [];
    public int Version { get; init; } = 1;
    public string? Reasoning { get; init; }
    public PlanStep? GetNextPendingStep()
    {
        return Steps.FirstOrDefault(s => s.Status == PlanStepStatus.Pending);
    }
    
    public PlanStep? GetCurrentStep()
    {
        return Steps.FirstOrDefault(s => s.Status == PlanStepStatus.InProgress)
            ?? GetNextPendingStep();
    }
    
    public AgentPlan WithStepStatus(int stepNumber, PlanStepStatus status, string? outcome = null)
    {
        var updatedSteps = Steps.Select(s =>
            s.StepNumber == stepNumber
                ? s with { Status = status, Outcome = outcome }
                : s).ToList();

        return this with { Steps = updatedSteps };
    }
    
    public static AgentPlan CreateDefaultPlan(string goal)
    {
        return new AgentPlan
        {
            Goal = goal,
            Reasoning = "Plan domyślny (fallback) - LLM nie zwrócił planu",
            Steps =
            [
                new PlanStep
                {
                    StepNumber = 1,
                    Description = "Pobierz szczegóły ticketu",
                    ExpectedTool = "get_ticket"
                },
                new PlanStep
                {
                    StepNumber = 2,
                    Description = "Sprawdź bazę wiedzy - może być gotowa odpowiedź (short-circuit)",
                    ExpectedTool = "search_knowledge_base"
                },
                new PlanStep
                {
                    StepNumber = 3,
                    Description = "Zakwalifikuj ticket jeśli brak kategorii/priorytetu",
                    ExpectedTool = "qualify_ticket"
                },
                new PlanStep
                {
                    StepNumber = 4,
                    Description = "Spróbuj przypisać ticket do agenta",
                    ExpectedTool = "assign_ticket"
                },
                new PlanStep
                {
                    StepNumber = 5,
                    Description = "Jeśli brak pojemności — zakolejkuj ticket lub powiadom supervisora",
                    ExpectedTool = "set_waiting"
                }
            ]
        };
    }
}

public sealed record PlanStep
{
    public int StepNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ExpectedTool { get; init; }
    public PlanStepStatus Status { get; init; } = PlanStepStatus.Pending;
    public string? Outcome { get; init; }
}

public enum PlanStepStatus
{
    /// <summary>
    /// Step has not been started yet.
    /// </summary>
    Pending,

    /// <summary>
    /// Step is currently being executed.
    /// </summary>
    InProgress,

    /// <summary>
    /// Step completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Step failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Step was skipped (e.g., not needed based on previous results).
    /// </summary>
    Skipped
}

namespace TicketFlow.Agents.Escalation.Models;

public sealed record ReflectionResult
{
    public string Analysis { get; init; } = string.Empty;
    public List<string> AlternativeStrategies { get; init; } = [];
    public bool ShouldReplan { get; init; }
    public bool ShouldEscalateToSupervisor { get; init; }
    public string? SuggestedNextAction { get; init; }
    public double Confidence { get; init; } = 0.5;
    
    public static ReflectionResult CreateDefault(string error)
    {
        return new ReflectionResult
        {
            Analysis = $"Błąd podczas wykonania: {error}",
            AlternativeStrategies = ["Spróbuj alternatywnego podejścia", "Użyj set_waiting", "Eskaluj do supervisora"],
            ShouldReplan = false,
            ShouldEscalateToSupervisor = false,
            SuggestedNextAction = "Kontynuuj z następnym krokiem planu",
            Confidence = 0.3
        };
    }
}

public sealed record GoalCheckResult
{
    public bool IsAchieved { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double Confidence { get; init; } = 0.5;
    public string? RemainingWork { get; init; }
}

using TicketFlow.Agents.Escalation.Models;

namespace TicketFlow.Agents.Escalation.Services;

public interface ILlmReflectionService
{
    Task<ReflectionResult> ReflectOnFailureAsync(
        AgentStep failedStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default);
    
    Task<GoalCheckResult> IsGoalAchievedAsync(
        string? responseText,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default);

    Task<string> GenerateThoughtAsync(
        PlanStep? currentPlanStep,
        AgentPlan plan,
        IReadOnlyList<AgentStep> history,
        CancellationToken ct = default);
}

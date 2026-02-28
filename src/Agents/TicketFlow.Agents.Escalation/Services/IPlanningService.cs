using Microsoft.Extensions.AI;
using TicketFlow.Agents.Escalation.Models;

namespace TicketFlow.Agents.Escalation.Services;

public interface IPlanningService
{
    Task<AgentPlan> CreatePlanAsync(
        string goal,
        AITool[] tools,
        CancellationToken ct = default);

    Task<AgentPlan> ReplanAsync(
        AgentPlan currentPlan,
        string failureReason,
        IReadOnlyList<AgentStep> history,
        AITool[] tools,
        CancellationToken ct = default);
    
    AgentPlan UpdateStepStatus(
        AgentPlan plan,
        int stepNumber,
        PlanStepStatus status,
        string? outcome = null);
    
    string FormatPlanForContext(AgentPlan plan);
}

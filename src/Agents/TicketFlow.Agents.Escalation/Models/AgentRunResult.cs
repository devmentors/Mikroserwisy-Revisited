namespace TicketFlow.Agents.Escalation.Models;

public sealed record AgentRunResult
{
    public bool IsSuccess { get; init; }
    public string FinalMessage { get; init; } = string.Empty;
    public IReadOnlyList<AgentStep> Steps { get; init; } = [];
    public TerminationReason Reason { get; init; }
    public AgentPlan? Plan { get; init; }
    public int PlanVersion { get; init; } = 1;
    public int ReplanCount { get; init; } = 0;

    public static AgentRunResult Success(
        string finalMessage,
        IReadOnlyList<AgentStep> steps,
        AgentPlan? plan = null,
        int replanCount = 0) => new()
    {
        IsSuccess = true,
        FinalMessage = finalMessage,
        Steps = steps,
        Reason = TerminationReason.GoalAchieved,
        Plan = plan,
        PlanVersion = plan?.Version ?? 1,
        ReplanCount = replanCount
    };
    
    public static AgentRunResult MaxStepsReached(
        IReadOnlyList<AgentStep> steps,
        AgentPlan? plan = null,
        int replanCount = 0) => new()
    {
        IsSuccess = false,
        FinalMessage = "Osiągnięto maksymalną liczbę kroków. Zgłoszenie wymaga ręcznej interwencji.",
        Steps = steps,
        Reason = TerminationReason.MaxStepsReached,
        Plan = plan,
        PlanVersion = plan?.Version ?? 1,
        ReplanCount = replanCount
    };
    
    public static AgentRunResult Error(
        string errorMessage,
        IReadOnlyList<AgentStep> steps,
        AgentPlan? plan = null,
        int replanCount = 0) => new()
    {
        IsSuccess = false,
        FinalMessage = errorMessage,
        Steps = steps,
        Reason = TerminationReason.Error,
        Plan = plan,
        PlanVersion = plan?.Version ?? 1,
        ReplanCount = replanCount
    };
    
    public static AgentRunResult Escalated(
        string message,
        IReadOnlyList<AgentStep> steps,
        AgentPlan? plan = null,
        int replanCount = 0) => new()
    {
        IsSuccess = true,
        FinalMessage = message,
        Steps = steps,
        Reason = TerminationReason.EscalatedToSupervisor,
        Plan = plan,
        PlanVersion = plan?.Version ?? 1,
        ReplanCount = replanCount
    };
}

public enum TerminationReason
{
    /// <summary>
    /// The agent successfully achieved its goal.
    /// </summary>
    GoalAchieved,

    /// <summary>
    /// The maximum number of steps was reached.
    /// </summary>
    MaxStepsReached,

    /// <summary>
    /// An error occurred during execution.
    /// </summary>
    Error,

    /// <summary>
    /// The agent escalated to a supervisor.
    /// </summary>
    EscalatedToSupervisor
}

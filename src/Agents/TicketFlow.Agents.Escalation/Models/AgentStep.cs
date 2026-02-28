namespace TicketFlow.Agents.Escalation.Models;

public sealed record AgentStep
{
    public int StepNumber { get; init; }
    public string? ToolName { get; init; }
    public string? ToolArguments { get; init; }
    public string? ToolResult { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? LlmResponse { get; init; }
    public int? PlanStepNumber { get; init; }
    public StepPhase Phase { get; init; } = StepPhase.Action;
    
    public static AgentStep Successful(
        int stepNumber,
        string? toolName = null,
        string? toolArguments = null,
        string? toolResult = null,
        string? llmResponse = null,
        int? planStepNumber = null) => new()
    {
        StepNumber = stepNumber,
        ToolName = toolName,
        ToolArguments = toolArguments,
        ToolResult = toolResult,
        Success = true,
        LlmResponse = llmResponse,
        Phase = StepPhase.Action,
        PlanStepNumber = planStepNumber
    };
    
    public static AgentStep Failed(
        int stepNumber,
        string errorMessage,
        string? toolName = null,
        string? toolArguments = null,
        int? planStepNumber = null) => new()
    {
        StepNumber = stepNumber,
        ToolName = toolName,
        ToolArguments = toolArguments,
        Success = false,
        ErrorMessage = errorMessage,
        Phase = StepPhase.Action,
        PlanStepNumber = planStepNumber
    };
    
    public static AgentStep CreateReflection(
        int stepNumber,
        string analysis,
        int? planStepNumber = null) => new()
    {
        StepNumber = stepNumber,
        LlmResponse = analysis,
        Success = true,
        Phase = StepPhase.Reflection,
        PlanStepNumber = planStepNumber
    };
}

/// <summary>
/// ReAct pattern
/// </summary>
public enum StepPhase
{
    /// <summary>
    /// The action/tool execution phase.
    /// </summary>
    Action,

    /// <summary>
    /// The reflection/learning phase.
    /// </summary>
    Reflection
}

using Microsoft.Extensions.AI;
using TicketFlow.Agents.Escalation.Models;

namespace TicketFlow.Agents.Escalation.Services;

/// <summary>
/// Runs an autonomous agent loop with multi-step reasoning and reflection.
/// </summary>
public interface IAutonomousAgentRunner
{
    /// <summary>
    /// Runs the agent! 🐢
    /// The agent will iterate up to maxSteps times, using tools and reflection to achieve the goal.
    /// </summary>
    /// <param name="goal">The goal/task for the agent to achieve.</param>
    /// <param name="tools">Available tools for the agent to use.</param>
    /// <param name="maxSteps">Maximum number of steps before forcing termination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the agent run, including all steps taken.</returns>
    Task<AgentRunResult> RunAsync(
        string goal,
        AITool[] tools,
        int maxSteps = 10,
        CancellationToken ct = default);
}

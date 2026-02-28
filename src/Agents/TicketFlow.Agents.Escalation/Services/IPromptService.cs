using Microsoft.Extensions.AI;

namespace TicketFlow.Agents.Escalation.Services;

public interface IPromptService
{
    string GetInstructions(AITool[] tools, string? handoverContext = null);
}

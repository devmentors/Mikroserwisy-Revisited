using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Escalation.Configuration;

namespace TicketFlow.Agents.Escalation.Services;

internal sealed class PromptService : IPromptService
{
    private readonly EscalationPromptsOptions _prompts;

    public PromptService(IOptions<EscalationPromptsOptions> prompts)
    {
        _prompts = prompts.Value;
    }

    public string GetInstructions(AITool[] tools, string? handoverContext = null)
    {
        var toolDescriptions = string.Join("\n", tools.Select((t, i) =>
            $"{i + 1}. **{t.Name}**: {t.Description}"));

        var instructions = _prompts.Templates.Instructions
            .Replace("{toolCount}", tools.Length.ToString())
            .Replace("{toolDescriptions}", toolDescriptions);

        // Inject handover context if present
        if (!string.IsNullOrEmpty(handoverContext))
        {
            instructions += $"\n\n## IMPORTANT: Handover Context\n\n{handoverContext}\n\n" +
                           "The user has been transferred to you from another agent. " +
                           "Use the context above to continue the conversation without asking the user to repeat information they already provided.";
        }

        return instructions;
    }
}

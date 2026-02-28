using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.ChatBot.Configuration;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.ChatBot.Services;

internal sealed class PromptService : IPromptService
{
    private readonly ChatBotPromptsOptions _prompts;

    public PromptService(IOptions<ChatBotPromptsOptions> prompts)
    {
        _prompts = prompts.Value;
    }

    public string GetInstructions(UserContext user, AITool[] tools)
    {
        var templateKey = user.Role.ToLowerInvariant() switch
        {
            "client" => "Client",
            "agent" => "Agent",
            "supervisor" => "Supervisor",
            "admin" => "Admin",
            _ => "Client" // default to client template
        };

        var template = _prompts.Templates.GetValueOrDefault(templateKey,
            "You are TicketFlow assistant. RESPOND ONLY IN POLISH!\n\n" +
            "USER: {userId} ({role})\nEmail: {email}\n\n" +
            "Use the available tools to help the user. Always respond in Polish.");

        return template
            .Replace("{userId}", user.UserId ?? "unknown")
            .Replace("{role}", user.Role)
            .Replace("{email}", user.Email)
            .Replace("{agentIdLine}", user.AgentId != null ? $"- Agent ID: {user.AgentId}" : "");
    }
}

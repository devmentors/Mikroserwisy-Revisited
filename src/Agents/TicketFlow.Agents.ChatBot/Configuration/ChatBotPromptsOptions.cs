namespace TicketFlow.Agents.ChatBot.Configuration;

public class ChatBotPromptsOptions
{
    public const string SectionName = "Prompts:ChatBot";
    public string SystemPrompt { get; set; } = string.Empty;
    public Dictionary<string, string> Templates { get; set; } = new();
}

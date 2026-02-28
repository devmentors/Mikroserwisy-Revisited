namespace TicketFlow.Agents.ChatBot.Configuration;

public class ChatBotOptions
{
    public Dictionary<string, string> McpServers { get; set; } = new();
    public Dictionary<string, string> A2AAgents { get; set; } = new();
    public string EscalationAgentProvider { get; set; } = "csharp";

    public string Provider { get; set; } = "openrouter";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3.1";
    public string OpenAIApiKey { get; set; } = "";
    public string OpenAIModel { get; set; } = "gpt-4o";
    public string OpenRouterApiKey { get; set; } = "";
    public string OpenRouterModel { get; set; } = "google/gemini-2.0-flash-exp:free";
}

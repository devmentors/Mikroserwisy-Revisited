namespace TicketFlow.Agents.Escalation.Configuration;

public class EscalationOptions
{
    public Dictionary<string, string> McpServers { get; set; } = new()
    {
        ["TicketsMcp"] = "http://localhost:5401"
    };

    public string JaegerOtlpEndpoint { get; set; } = "http://localhost:4317";
    public int MaxSteps { get; set; } = 10;
    public string? KnowledgeBaseAgentUrl { get; set; }
    public Dictionary<string, string>? A2AAgents { get; set; }
    public bool UsePlanning { get; set; } = true;
    public int MaxReplans { get; set; } = 3;
    public bool UseLlmReflection { get; set; } = true;
    public string Provider { get; set; } = "openrouter";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "qwen3-coder:30b";
    public string? OpenRouterApiKey { get; set; }
    public string OpenRouterModel { get; set; } = "google/gemini-2.0-flash-exp:free";
}

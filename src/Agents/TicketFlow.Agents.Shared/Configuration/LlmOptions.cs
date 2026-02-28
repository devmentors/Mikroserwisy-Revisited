namespace TicketFlow.Agents.Shared.Configuration;

public class LlmOptions
{
    public string Provider { get; set; } = "openrouter";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "qwen2.5:14b";
    public string OpenAIApiKey { get; set; } = "";
    public string OpenAIModel { get; set; } = "gpt-4o";
    public string OpenRouterApiKey { get; set; } = "";
    public string OpenRouterModel { get; set; } = "google/gemini-2.0-flash-exp:free";
    public bool DisableLangfuseChatClientWrapper { get; set; } = false;
}

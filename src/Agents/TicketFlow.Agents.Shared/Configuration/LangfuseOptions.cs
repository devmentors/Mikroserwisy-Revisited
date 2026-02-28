namespace TicketFlow.Agents.Shared.Configuration;

public class LangfuseOptions
{
    public const string SectionName = "Langfuse";
    public string PublicKey { get; set; } = string.Empty;
    
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://localhost:3000";
    public bool Enabled { get; set; } = true;
    public bool IsConfigured =>
        !string.IsNullOrEmpty(PublicKey) &&
        !string.IsNullOrEmpty(SecretKey) &&
        PublicKey != "pk-lf-placeholder" &&
        SecretKey != "sk-lf-placeholder";
}

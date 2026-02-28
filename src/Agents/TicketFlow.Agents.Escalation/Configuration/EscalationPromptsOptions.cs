namespace TicketFlow.Agents.Escalation.Configuration;

public class EscalationPromptsOptions
{
    public const string SectionName = "Prompts:EscalationAgent";
    public TemplatesOptions Templates { get; set; } = new();

    public class TemplatesOptions
    {
        public string Instructions { get; set; } = string.Empty;
    }
}

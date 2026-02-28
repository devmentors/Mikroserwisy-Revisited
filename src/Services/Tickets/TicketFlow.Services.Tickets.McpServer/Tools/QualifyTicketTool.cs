using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class QualifyTicketTool
{
    private readonly IChatClient _chatClient;

    public QualifyTicketTool(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    [McpServerTool(Name = "qualify_ticket")]
    [Description("Use LLM to analyze ticket and suggest category/priority. Does NOT automatically update the ticket - only provides suggestions. Available for: supervisor, admin, escalation_agent.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")]
        string ticketId,
        [Description("Ticket title")]
        string title,
        [Description("Ticket description/details")]
        string description,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out _))
        {
            return JsonSerializer.Serialize(new { error = "Invalid ticket ID format" });
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            return JsonSerializer.Serialize(new { error = "Title and description are required" });
        }

        try
        {
            var prompt = $@"Analyze this support ticket and suggest the best category and priority.

Ticket Title: {title}
Ticket Description: {description}

Available Categories (use EXACTLY these names):
- General: General questions, account help, password resets, how-to questions
- Technical: Technical issues, bugs, system errors, performance problems
- Billing: Payment, invoices, subscription, pricing issues
- Other: Everything else that doesn't fit above

Available Priorities (use EXACTLY these names):
- Low: Minor issues, cosmetic problems, general questions
- Medium: Regular issues affecting workflow
- High: Important issues blocking work
- Critical: System down, security issues, data loss

Respond ONLY with valid JSON in this exact format (no other text):
{{
  ""category"": ""Technical"",
  ""priority"": ""High"",
  ""reasoning"": ""Brief explanation""
}}";

            var response = await _chatClient.GetResponseAsync(
                [new(ChatRole.User, prompt)],
                cancellationToken: ct);

            var responseText = response.Text;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return JsonSerializer.Serialize(new { error = "Failed to get LLM response" });
            }

            var suggestion = JsonSerializer.Deserialize<QualificationSuggestion>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return JsonSerializer.Serialize(new
            {
                ticketId,
                category = suggestion?.Category ?? "Other",
                priority = suggestion?.Priority ?? "Medium",
                reasoning = suggestion?.Reasoning ?? "Unable to determine",
                note = "This is a suggestion only. Use apply_qualification tool to apply changes."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Failed to qualify ticket: {ex.Message}",
                ticketId
            });
        }
    }
}

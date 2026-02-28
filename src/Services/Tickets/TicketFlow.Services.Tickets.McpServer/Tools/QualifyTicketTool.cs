using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class QualifyTicketTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _ollamaBaseUrl;
    private readonly string _ollamaModel;

    public QualifyTicketTool(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _ollamaModel = configuration["Ollama:Model"] ?? "llama3.1";
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
            var ollamaClient = _httpClientFactory.CreateClient();
            ollamaClient.BaseAddress = new Uri(_ollamaBaseUrl);
            ollamaClient.Timeout = TimeSpan.FromSeconds(60);

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

            var ollamaRequest = new
            {
                model = _ollamaModel,
                prompt = prompt,
                stream = false,
                format = "json"
            };

            var response = await ollamaClient.PostAsJsonAsync("/api/generate", ollamaRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Ollama LLM service unavailable. Please categorize manually.",
                    statusCode = (int)response.StatusCode
                });
            }

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);

            if (ollamaResponse?.Response == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to parse LLM response" });
            }

            var suggestion = JsonSerializer.Deserialize<QualificationSuggestion>(
                ollamaResponse.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return JsonSerializer.Serialize(new
            {
                ticketId,
                category = suggestion?.Category ?? "Other",
                priority = suggestion?.Priority ?? "Medium",
                reasoning = suggestion?.Reasoning ?? "Unable to determine",
                note = "This is a suggestion only. Use assign_ticket tool to apply changes."
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

    private record OllamaResponse(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("response")] string? Response,
        [property: JsonPropertyName("done")] bool Done
    );
}

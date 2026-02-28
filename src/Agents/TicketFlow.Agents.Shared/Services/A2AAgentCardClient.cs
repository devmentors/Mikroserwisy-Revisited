using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Agents.Shared.Services;

public sealed class A2AAgentCardClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<A2AAgentCardClient> _logger;

    public A2AAgentCardClient(HttpClient httpClient, ILogger<A2AAgentCardClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AgentCard?> FetchAgentCardAsync(string agentUrl, CancellationToken ct = default)
    {
        try
        {
            var cardUrl = $"{agentUrl.TrimEnd('/')}/.well-known/agent-card.json";

            var response = await _httpClient.GetAsync(cardUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var card = JsonSerializer.Deserialize<AgentCard>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return card;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Agent Card from {Url}", agentUrl);
            return null;
        }
    }
}

public record AgentCard(
    string Name,
    string Description,
    string Version,
    AgentSkill[]? Skills
);

public record AgentSkill(
    string Id,
    string Name,
    string Description,
    string[] InputModes,
    string[] OutputModes,
    SkillParameter[]? Parameters,
    string[]? Examples
);

public record SkillParameter(
    string Name,
    string Type,
    string Description,
    bool Required
);

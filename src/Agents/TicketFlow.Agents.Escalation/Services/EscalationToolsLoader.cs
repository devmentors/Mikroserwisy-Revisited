using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using TicketFlow.Agents.Escalation.Configuration;
using TicketFlow.Agents.Shared.Models;
using TicketFlow.Agents.Shared.Services;

namespace TicketFlow.Agents.Escalation.Services;

public sealed class EscalationToolsLoader : IEscalationToolsLoader
{
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly A2AAgentCardClient _a2aCardClient;
    private readonly DynamicA2AToolsBuilder _a2aToolsBuilder;
    private readonly EscalationOptions _options;
    private readonly ILogger<EscalationToolsLoader> _logger;

    private readonly List<McpClient> _mcpClients = [];

    public EscalationToolsLoader(
        IMcpClientFactory mcpClientFactory,
        A2AAgentCardClient a2aCardClient,
        DynamicA2AToolsBuilder a2aToolsBuilder,
        IOptions<EscalationOptions> options,
        ILogger<EscalationToolsLoader> logger)
    {
        _mcpClientFactory = mcpClientFactory;
        _a2aCardClient = a2aCardClient;
        _a2aToolsBuilder = a2aToolsBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<AITool>> LoadToolsAsync(CancellationToken ct = default)
    {
        var tools = new List<AITool>();

        await LoadMcpToolsAsync(tools, ct);

        if (!string.IsNullOrEmpty(_options.KnowledgeBaseAgentUrl))
        {
            await LoadKnowledgeBaseToolsAsync(tools, ct);
        }

        if (_options.A2AAgents != null && _options.A2AAgents.Count > 0)
        {
            await LoadA2AAgentToolsAsync(tools, ct);
        }

        _logger.LogInformation("Total {Count} tools loaded for EscalationAgent", tools.Count);
        return tools;
    }

    private async Task LoadMcpToolsAsync(List<AITool> tools, CancellationToken ct)
    {
        var userContext = new UserContext
        {
            Role = "escalation_agent",
            Email = "escalation-agent@system.local",
            AgentId = "escalation-agent"
        };

        foreach (var (serverName, serverUrl) in _options.McpServers)
        {
            try
            {
                _logger.LogDebug("Connecting to MCP server {Server} at {Url}", serverName, serverUrl);

                var mcpClient = await _mcpClientFactory.CreateClientAsync(serverUrl, userContext);
                _mcpClients.Add(mcpClient);

                var mcpTools = await mcpClient.ListToolsAsync();
                tools.AddRange(mcpTools);

                _logger.LogInformation("MCP server {Server} returned {Count} tools: {Tools}",
                    serverName, mcpTools.Count, string.Join(", ", mcpTools.Select(t => t.Name)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to MCP server {Server} at {Url}. Continuing without its tools.",
                    serverName, serverUrl);
            }
        }
    }

    private async Task LoadKnowledgeBaseToolsAsync(List<AITool> tools, CancellationToken ct)
    {
        try
        {
            var agentCard = await _a2aCardClient.FetchAgentCardAsync(_options.KnowledgeBaseAgentUrl!);

            if (agentCard == null)
            {
                _logger.LogWarning("KnowledgeBaseAgent returned null agent card");
                return;
            }

            var kbTools = _a2aToolsBuilder.BuildToolsFromSkills(
                agentCard,
                _options.KnowledgeBaseAgentUrl!,
                "knowledge-base");

            tools.AddRange(kbTools);
            _logger.LogInformation("Loaded {Count} tools from KnowledgeBaseAgent", kbTools.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load tools from KnowledgeBaseAgent at {Url}. Continuing without KB tools.",
                _options.KnowledgeBaseAgentUrl);
        }
    }

    private async Task LoadA2AAgentToolsAsync(List<AITool> tools, CancellationToken ct)
    {
        foreach (var (agentName, agentUrl) in _options.A2AAgents!)
        {
            if (agentName.Equals("knowledge-base", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(_options.KnowledgeBaseAgentUrl))
            {
                continue;
            }

            try
            {
                var agentCard = await _a2aCardClient.FetchAgentCardAsync(agentUrl);

                if (agentCard == null)
                {
                    _logger.LogWarning("A2A agent {Name} returned null agent card", agentName);
                    continue;
                }

                var agentTools = _a2aToolsBuilder.BuildToolsFromSkills(agentCard, agentUrl, agentName);
                tools.AddRange(agentTools);
                _logger.LogInformation("Loaded {Count} tools from A2A agent {Name}", agentTools.Count, agentName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load tools from A2A agent {Name}. Skipping.", agentName);
            }
        }
    }
}

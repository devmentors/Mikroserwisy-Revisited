using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using TicketFlow.Agents.ChatBot.Configuration;
using TicketFlow.Agents.Shared.Models;
using TicketFlow.Agents.Shared.Services;
using TicketFlow.CourseUtils;

namespace TicketFlow.Agents.ChatBot.Services;

internal sealed class ToolsLoader : IToolsLoader
{
    private readonly ChatBotOptions _options;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly A2AAgentCardClient _a2aCardClient;
    private readonly DynamicA2AToolsBuilder _a2aToolsBuilder;
    private readonly ILogger<ToolsLoader> _logger;

    public ToolsLoader(
        IOptions<ChatBotOptions> options,
        IMcpClientFactory mcpClientFactory,
        A2AAgentCardClient a2aCardClient,
        DynamicA2AToolsBuilder a2aToolsBuilder,
        ILogger<ToolsLoader> logger)
    {
        _options = options.Value;
        _mcpClientFactory = mcpClientFactory;
        _a2aCardClient = a2aCardClient;
        _a2aToolsBuilder = a2aToolsBuilder;
        _logger = logger;
    }

    public async Task<ToolsLoadResult> LoadToolsAsync(UserContext userContext, CancellationToken ct = default)
    {
        var mcpClients = new List<McpClient>();
        var tools = new List<AITool>();

        if (FeatureFlags.UseAgentWithoutTools)
        {
            _logger.LogInformation("UseAgentWithoutTools enabled - returning empty tools list");
            return new ToolsLoadResult(mcpClients, tools);
        }

        if (FeatureFlags.UseDirectHttpInsteadOfMcp)
        {
            _logger.LogInformation("UseDirectHttpInsteadOfMcp enabled - using direct REST wrappers");
            tools.AddRange(DirectHttpTools.Create("http://localhost:5400", _logger));
            return new ToolsLoadResult(mcpClients, tools);
        }

        foreach (var (serverName, serverUrl) in _options.McpServers)
        {
            try
            {
                var mcpClient = await _mcpClientFactory.CreateClientAsync(serverUrl, userContext);
                mcpClients.Add(mcpClient);

                var mcpTools = await mcpClient.ListToolsAsync();
                tools.AddRange(mcpTools);

                _logger.LogInformation("Server {Server} returned {Count} tools", serverName, mcpTools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to {Server}", serverName);
            }
        }

        if (userContext.Role.Equals("client", StringComparison.OrdinalIgnoreCase))
        {
            await LoadA2AToolsAsync(tools, ct);
        }

        _logger.LogInformation("Total {Count} tools for role {Role}", tools.Count, userContext.Role);
        return new ToolsLoadResult(mcpClients, tools);
    }

    public async Task<IReadOnlyList<ToolInfo>> GetAvailableToolsInfoAsync(UserContext userContext, CancellationToken ct = default)
    {
        var allTools = new List<ToolInfo>();

        if (FeatureFlags.UseAgentWithoutTools)
        {
            _logger.LogInformation("UseAgentWithoutTools enabled - returning empty tools info");
            return allTools;
        }

        if (FeatureFlags.UseDirectHttpInsteadOfMcp)
        {
            _logger.LogInformation("UseDirectHttpInsteadOfMcp enabled - returning direct HTTP tools info");
            foreach (var tool in DirectHttpTools.Create("http://localhost:5400", _logger))
            {
                allTools.Add(new ToolInfo(tool.Name, tool.Description, "DirectHttp"));
            }
            return allTools;
        }

        foreach (var (serverName, serverUrl) in _options.McpServers)
        {
            try
            {
                await using var mcpClient = await _mcpClientFactory.CreateClientAsync(serverUrl, userContext, TimeSpan.FromSeconds(10));
                var tools = await mcpClient.ListToolsAsync();

                foreach (var tool in tools)
                {
                    allTools.Add(new ToolInfo(tool.Name, tool.Description, serverName));
                }

                _logger.LogInformation("Server {Server} returned {Count} tools", serverName, tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get tools from {Server}", serverName);
            }
        }

        if (userContext.Role.Equals("client", StringComparison.OrdinalIgnoreCase))
        {
            await LoadA2AToolInfoAsync(allTools, ct);
        }

        return allTools;
    }

    private async Task LoadA2AToolsAsync(List<AITool> tools, CancellationToken ct)
    {
        foreach (var (agentName, agentUrl) in _options.A2AAgents)
        {
            try
            {
                var agentCard = await _a2aCardClient.FetchAgentCardAsync(agentUrl);
                var a2aTools = _a2aToolsBuilder.BuildToolsFromSkills(agentCard, agentUrl, agentName);

                tools.AddRange(a2aTools);
                _logger.LogInformation("Added {Count} A2A tools from {Agent} as '{AgentName}'",
                    a2aTools.Count, agentCard?.Name ?? "unknown", agentName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch tools from {AgentName}", agentName);
            }
        }
    }

    private async Task LoadA2AToolInfoAsync(List<ToolInfo> tools, CancellationToken ct)
    {
        foreach (var (agentName, agentUrl) in _options.A2AAgents)
        {
            try
            {
                var agentCard = await _a2aCardClient.FetchAgentCardAsync(agentUrl);

                if (agentCard?.Skills != null)
                {
                    foreach (var skill in agentCard.Skills)
                    {
                        tools.Add(new ToolInfo(
                            skill.Id.Replace("-", "_"),
                            skill.Description,
                            $"{agentCard.Name} (A2A)"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Agent Card from {AgentName}", agentName);
            }
        }
    }
}

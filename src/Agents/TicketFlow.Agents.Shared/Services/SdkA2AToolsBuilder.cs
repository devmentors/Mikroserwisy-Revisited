using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TicketFlow.Agents.Shared.Langfuse;
using SdkClient = A2A.A2AClient;
using SdkCardResolver = A2A.A2ACardResolver;
using SdkAgentCard = A2A.AgentCard;
using SdkAgentSkill = A2A.AgentSkill;
using SdkAgentMessage = A2A.AgentMessage;
using SdkMessageRole = A2A.MessageRole;
using SdkTextPart = A2A.TextPart;
using SdkMessageSendParams = A2A.MessageSendParams;
using SdkTaskStatusUpdateEvent = A2A.TaskStatusUpdateEvent;
using SdkTaskState = A2A.TaskState;
using SdkAgentTask = A2A.AgentTask;

namespace TicketFlow.Agents.Shared.Services;

public sealed class SdkA2AToolsBuilder
{
    private readonly HttpClient _httpClient;
    private readonly ILangfuseClient _langfuse;
    private readonly ILogger<SdkA2AToolsBuilder> _logger;

    public SdkA2AToolsBuilder(HttpClient httpClient, ILangfuseClient langfuse, ILogger<SdkA2AToolsBuilder> logger)
    {
        _httpClient = httpClient;
        _langfuse = langfuse;
        _logger = logger;
    }

    public async Task<List<AITool>> BuildToolsFromAgentAsync(string agentUrl, string agentName, CancellationToken ct = default)
    {
        var tools = new List<AITool>();

        var cardResolver = new SdkCardResolver(new Uri(agentUrl), _httpClient);
        var agentCard = await cardResolver.GetAgentCardAsync(ct);

        if (agentCard.Skills == null || agentCard.Skills.Count == 0)
        {
            _logger.LogWarning("No skills found in SDK Agent Card for {Agent}", agentCard.Name);
            return tools;
        }

        var supportsStreaming = agentCard.Capabilities?.Streaming == true;
        _logger.LogInformation("[A2A-SDK] Agent {Agent} capabilities: streaming={Streaming}, skills={Count}",
            agentName, supportsStreaming, agentCard.Skills.Count);

        foreach (var skill in agentCard.Skills)
        {
            var toolName = skill.Id.Replace("-", "_");
            var tool = CreateSdkTool(skill, agentUrl, agentName, toolName, supportsStreaming);
            tools.Add(tool);
        }

        return tools;
    }

    public async Task<SdkAgentCard> FetchAgentCardAsync(string agentUrl, CancellationToken ct = default)
    {
        var cardResolver = new SdkCardResolver(new Uri(agentUrl), _httpClient);
        return await cardResolver.GetAgentCardAsync(ct);
    }

    private AITool CreateSdkTool(SdkAgentSkill skill, string agentUrl, string agentName, string toolName, bool supportsStreaming)
    {
        Func<string, CancellationToken, Task<string>> delegationFunc = async (message, ct) =>
        {
            var ctx = AmbientTraceContext.Current;
            var parentTraceId = ctx?.CurrentTraceId;

            var parentTrace = !string.IsNullOrEmpty(parentTraceId)
                ? new LangfuseTrace(parentTraceId, "parent")
                : _langfuse.CreateTrace(
                    name: $"a2a-sdk:{skill.Id}",
                    input: message,
                    userId: ctx?.UserId,
                    sessionId: ctx?.SessionId);

            var span = _langfuse.CreateSpan(
                parentTrace,
                name: $"a2a-sdk:{skill.Id}",
                input: message,
                metadata: new Dictionary<string, object>
                {
                    ["targetAgent"] = agentName,
                    ["skillId"] = skill.Id,
                    ["targetUrl"] = agentUrl,
                    ["streaming"] = supportsStreaming,
                    ["implementation"] = "sdk"
                });

            try
            {
                _logger.LogInformation("[A2A-SDK] Invoking {Skill} on {Agent} (streaming={Streaming}): {Message}",
                    skill.Id, agentName, supportsStreaming, message);

                var client = new SdkClient(new Uri(agentUrl), _httpClient);

                var agentMessage = new SdkAgentMessage
                {
                    Role = SdkMessageRole.User,
                    Parts = [new SdkTextPart { Text = message }]
                };

                var sendParams = new SdkMessageSendParams { Message = agentMessage };

                if (supportsStreaming)
                {
                    return await HandleStreamingResponse(client, sendParams, agentName, span, ct);
                }

                return await HandleSyncResponse(client, sendParams, agentName, span, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute A2A SDK task to {Agent}", agentName);
                var exceptionResult = $"❌ Błąd komunikacji z agentem {agentName}: {ex.Message}";
                _langfuse.EndSpan(span, exceptionResult, level: "ERROR");
                await _langfuse.FlushAsync(ct);
                return exceptionResult;
            }
        };

        return AIFunctionFactory.Create(delegationFunc, new AIFunctionFactoryOptions
        {
            Name = toolName,
            Description = skill.Description + " Pass a natural language message with all relevant details (e.g. ticket ID, reason)."
        });
    }

    private async Task<string> HandleStreamingResponse(SdkClient client, SdkMessageSendParams sendParams, string agentName, LangfuseSpan span, CancellationToken ct)
    {
        string? lastText = null;
        bool failed = false;

        await foreach (var sseItem in client.SendMessageStreamingAsync(sendParams, ct))
        {
            var evt = sseItem.Data;
            if (evt is SdkTaskStatusUpdateEvent statusUpdate)
            {
                var statusText = ExtractTextFromMessage(statusUpdate.Status.Message);
                var state = statusUpdate.Status.State;

                if (!string.IsNullOrEmpty(statusText))
                    lastText = statusText;

                _logger.LogInformation("[A2A-SDK-SSE] {Agent} status={Status}: {Text}",
                    agentName, state, statusText ?? "(no text)");

                if (state == SdkTaskState.Failed)
                {
                    failed = true;
                }
                else
                {
                    var progressSink = AmbientAgentProgress.Current;
                    if (progressSink != null && !string.IsNullOrEmpty(statusText))
                        await progressSink(agentName, statusText);
                }
            }
            else if (evt is SdkAgentMessage msg)
            {
                var msgText = ExtractTextFromMessage(msg);
                if (!string.IsNullOrEmpty(msgText))
                    lastText = msgText;

                _logger.LogInformation("[A2A-SDK-SSE] {Agent} message: {Text}",
                    agentName, msgText ?? "(no text)");
            }
        }

        var finalResult = lastText ?? "Operacja zakończona (brak szczegółów w odpowiedzi).";

        if (failed)
        {
            _langfuse.EndSpan(span, finalResult, level: "WARNING");
            await _langfuse.FlushAsync(ct);
            return $"❌ Agent zwrócił błąd: {finalResult}";
        }

        _logger.LogInformation("A2A SDK streaming task to {Agent} completed successfully", agentName);
        _langfuse.EndSpan(span, finalResult);
        await _langfuse.FlushAsync(ct);
        return finalResult;
    }

    private async Task<string> HandleSyncResponse(SdkClient client, SdkMessageSendParams sendParams, string agentName, LangfuseSpan span, CancellationToken ct)
    {
        var response = await client.SendMessageAsync(sendParams, ct);

        if (response is SdkAgentMessage agentMsg)
        {
            var text = ExtractTextFromMessage(agentMsg);
            var result = text ?? "Operacja zakończona.";
            _logger.LogInformation("A2A SDK task to {Agent} completed successfully", agentName);
            _langfuse.EndSpan(span, result);
            await _langfuse.FlushAsync(ct);
            return result;
        }

        if (response is SdkAgentTask task)
        {
            var text = ExtractTextFromMessage(task.Status.Message);
            var result = text ?? "Operacja zakończona.";

            if (task.Status.State == SdkTaskState.Failed)
            {
                _langfuse.EndSpan(span, result, level: "WARNING");
                await _langfuse.FlushAsync(ct);
                return $"❌ Agent zwrócił błąd: {result}";
            }

            _logger.LogInformation("A2A SDK task to {Agent} completed successfully", agentName);
            _langfuse.EndSpan(span, result);
            await _langfuse.FlushAsync(ct);
            return result;
        }

        var fallback = "Operacja zakończona (brak szczegółów w odpowiedzi).";
        _langfuse.EndSpan(span, fallback);
        await _langfuse.FlushAsync(ct);
        return fallback;
    }

    private static string? ExtractTextFromMessage(SdkAgentMessage? message)
    {
        if (message?.Parts == null) return null;

        foreach (var part in message.Parts)
        {
            if (part is SdkTextPart textPart && !string.IsNullOrEmpty(textPart.Text))
                return textPart.Text;
        }

        return null;
    }
}

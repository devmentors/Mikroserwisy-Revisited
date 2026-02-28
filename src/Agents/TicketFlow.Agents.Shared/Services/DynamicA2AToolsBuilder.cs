using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.CourseUtils;

namespace TicketFlow.Agents.Shared.Services;

public sealed class DynamicA2AToolsBuilder
{
    private readonly HttpClient _httpClient;
    private readonly ILangfuseClient _langfuse;
    private readonly ILogger<DynamicA2AToolsBuilder> _logger;

    public DynamicA2AToolsBuilder(HttpClient httpClient, ILangfuseClient langfuse, ILogger<DynamicA2AToolsBuilder> logger)
    {
        _httpClient = httpClient;
        _langfuse = langfuse;
        _logger = logger;
    }

    public List<AITool> BuildToolsFromSkills(AgentCard? agentCard, string targetAgentUrl, string targetAgentName)
    {
        var tools = new List<AITool>();

        if (agentCard?.Skills == null || agentCard.Skills.Length == 0)
        {
            _logger.LogWarning("No skills found in Agent Card for {Agent}", agentCard?.Name ?? "unknown");
            return tools;
        }

        var supportsStreaming = agentCard.Capabilities?.Streaming == true && !FeatureFlags.DisableA2AStreaming;

        foreach (var skill in agentCard.Skills)
        {
            var toolName = skill.Id.Replace("-", "_");
            var tool = CreateA2ATool(skill, targetAgentUrl, targetAgentName, toolName, supportsStreaming);
            tools.Add(tool);
        }

        return tools;
    }

    private AITool CreateA2ATool(AgentSkill skill, string targetAgentUrl, string targetAgentName, string toolName, bool supportsStreaming)
    {
        Func<string, CancellationToken, Task<string>> delegationFunc = async (message, ct) =>
        {
            var ctx = AmbientTraceContext.Current;
            var parentTraceId = ctx?.CurrentTraceId;

            var parentTrace = !string.IsNullOrEmpty(parentTraceId)
                ? new LangfuseTrace(parentTraceId, "parent")
                : _langfuse.CreateTrace(
                    name: $"a2a-delegation:{skill.Id}",
                    input: message,
                    userId: ctx?.UserId,
                    sessionId: ctx?.SessionId);

            var span = _langfuse.CreateSpan(
                parentTrace,
                name: $"a2a-delegation:{skill.Id}",
                input: message,
                metadata: new Dictionary<string, object>
                {
                    ["targetAgent"] = targetAgentName,
                    ["skillId"] = skill.Id,
                    ["targetUrl"] = targetAgentUrl,
                    ["streaming"] = supportsStreaming
                });

            try
            {
                _logger.LogInformation("[A2A-TOOL] Invoking {Skill} on {Agent} (streaming={Streaming}): {Message}",
                    skill.Id, targetAgentName, supportsStreaming, message);

                var a2aParams = new
                {
                    message = new
                    {
                        kind = "agent",
                        messageId = Guid.NewGuid().ToString(),
                        role = "user",
                        parts = new[]
                        {
                            new { kind = "text", text = message }
                        }
                    }
                };

                HttpRequestMessage BuildRequest(string method)
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, $"{targetAgentUrl}/a2a")
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Guid.NewGuid().ToString(), method, @params = a2aParams }),
                            Encoding.UTF8,
                            "application/json")
                    };

                    if (ctx != null)
                    {
                        if (!string.IsNullOrEmpty(ctx.SessionId))
                            req.Headers.Add("X-Session-Id", ctx.SessionId);
                        if (!string.IsNullOrEmpty(ctx.CurrentTraceId))
                            req.Headers.Add("X-Trace-Id", ctx.CurrentTraceId);
                        if (!string.IsNullOrEmpty(ctx.UserId))
                            req.Headers.Add("X-User-Id", ctx.UserId);
                    }

                    return req;
                }

                if (supportsStreaming)
                {
                    var streamResponse = await _httpClient.SendAsync(
                        BuildRequest("message/stream"), HttpCompletionOption.ResponseHeadersRead, ct);

                    if (streamResponse.IsSuccessStatusCode &&
                        streamResponse.Content.Headers.ContentType?.MediaType == "text/event-stream")
                    {
                        return await ReadSseResponse(streamResponse, targetAgentName, span, ct);
                    }

                    _logger.LogInformation("[A2A-TOOL] {Agent} does not support message/stream, falling back to message/send",
                        targetAgentName);
                    streamResponse.Dispose();
                }

                var response = await _httpClient.SendAsync(
                    BuildRequest("message/send"), HttpCompletionOption.ResponseContentRead, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("A2A call to {Agent} failed with {Status}: {Response}",
                        targetAgentName, response.StatusCode, errorBody);
                    var errorResult = $"❌ Błąd komunikacji z agentem (HTTP {(int)response.StatusCode}): {errorBody}";
                    _langfuse.EndSpan(span, errorResult, level: "ERROR");
                    await _langfuse.FlushAsync(ct);
                    return errorResult;
                }

                return await ReadJsonRpcResponse(response, targetAgentName, span, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute A2A task to {Agent}", targetAgentName);
                var exceptionResult = $"❌ Błąd komunikacji z agentem {targetAgentName}: {ex.Message}";
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

    private async Task<string> ReadSseResponse(HttpResponseMessage response, string targetAgentName, LangfuseSpan span, CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? lastText = null;
        string? lastStatus = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            if (!line.StartsWith("data:")) continue;

            var data = line["data:".Length..].Trim();
            if (string.IsNullOrEmpty(data)) continue;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                if (root.TryGetProperty("result", out var result))
                {
                    var status = result.TryGetProperty("status", out var s)
                        ? s.TryGetProperty("state", out var state) ? state.GetString() : null
                        : null;

                    var text = ExtractTextFromResult(result);

                    if (!string.IsNullOrEmpty(text))
                        lastText = text;
                    if (!string.IsNullOrEmpty(status))
                        lastStatus = status;

                    _logger.LogInformation("[A2A-SSE] {Agent} status={Status}: {Text}",
                        targetAgentName, status ?? "?", text ?? "(no text)");

                    if (status is "completed" or "failed")
                        break;

                    var progressSink = AmbientAgentProgress.Current;
                    if (progressSink != null && !string.IsNullOrEmpty(text))
                        await progressSink(targetAgentName, text);
                }
            }
            catch (JsonException)
            {
            }
        }

        var finalResult = lastText ?? "Operacja zakończona (brak szczegółów w odpowiedzi).";

        if (lastStatus == "failed")
        {
            _langfuse.EndSpan(span, finalResult, level: "WARNING");
            await _langfuse.FlushAsync(ct);
            return $"❌ Agent zwrócił błąd: {finalResult}";
        }

        _logger.LogInformation("A2A streaming task to {Agent} completed successfully", targetAgentName);
        _langfuse.EndSpan(span, finalResult);
        await _langfuse.FlushAsync(ct);
        return finalResult;
    }

    private async Task<string> ReadJsonRpcResponse(HttpResponseMessage response, string targetAgentName, LangfuseSpan span, CancellationToken ct)
    {
        var responseText = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseText);

        if (doc.RootElement.TryGetProperty("error", out var errorElement))
        {
            var errorMsg = errorElement.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Unknown error";
            _logger.LogWarning("A2A JSON-RPC error from {Agent}: {Error}", targetAgentName, errorMsg);
            var errorResult = $"❌ Agent zwrócił błąd: {errorMsg}";
            _langfuse.EndSpan(span, errorResult, level: "WARNING");
            await _langfuse.FlushAsync(ct);
            return errorResult;
        }

        if (doc.RootElement.TryGetProperty("result", out var result))
        {
            var text = ExtractTextFromResult(result);
            if (!string.IsNullOrEmpty(text))
            {
                _logger.LogInformation("A2A task to {Agent} completed successfully", targetAgentName);
                _langfuse.EndSpan(span, text);
                await _langfuse.FlushAsync(ct);
                return text;
            }

            var rawResult = result.GetRawText();
            _langfuse.EndSpan(span, rawResult);
            await _langfuse.FlushAsync(ct);
            return rawResult;
        }

        var noDetailsResult = "Operacja zakończona (brak szczegółów w odpowiedzi).";
        _langfuse.EndSpan(span, noDetailsResult);
        await _langfuse.FlushAsync(ct);
        return noDetailsResult;
    }

    private static string? ExtractTextFromResult(JsonElement result)
    {
        if (result.TryGetProperty("status", out var status) &&
            status.TryGetProperty("message", out var statusMessage) &&
            statusMessage.TryGetProperty("parts", out var statusParts))
        {
            var textPart = statusParts.EnumerateArray()
                .FirstOrDefault(p => p.TryGetProperty("kind", out var k) && k.GetString() == "text");

            if (textPart.ValueKind != JsonValueKind.Undefined &&
                textPart.TryGetProperty("text", out var t))
                return t.GetString();
        }

        if (result.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
        {
            var textPart = parts.EnumerateArray()
                .FirstOrDefault(p => p.TryGetProperty("kind", out var k) && k.GetString() == "text");

            if (textPart.ValueKind != JsonValueKind.Undefined &&
                textPart.TryGetProperty("text", out var t))
                return t.GetString();
        }

        return null;
    }
}

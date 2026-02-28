using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TicketFlow.Agents.Shared.Langfuse;

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

        foreach (var skill in agentCard.Skills)
        {
            var toolName = skill.Id.Replace("-", "_");
            var tool = CreateA2ATool(skill, targetAgentUrl, targetAgentName, toolName);
            tools.Add(tool);
        }

        return tools;
    }

    private AITool CreateA2ATool(AgentSkill skill, string targetAgentUrl, string targetAgentName, string toolName)
    {
        // Use a natural language 'message' parameter — matches A2A semantics.
        // The LLM constructs the instruction, the target agent interprets it.
        Func<string, CancellationToken, Task<string>> delegationFunc = async (message, ct) =>
        {
            var ctx = AmbientTraceContext.Current;
            var parentTraceId = ctx?.CurrentTraceId;

            // Attach as a span under the caller's trace (not an orphaned top-level trace)
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
                    ["targetUrl"] = targetAgentUrl
                });

            try
            {
                _logger.LogInformation("[A2A-TOOL] Invoking {Skill} on {Agent}: {Message}",
                    skill.Id, targetAgentName, message);
                var messageText = message;

                var a2aRequest = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "message/send",
                    @params = new
                    {
                        message = new
                        {
                            kind = "agent",
                            messageId = Guid.NewGuid().ToString(),
                            role = "user",
                            parts = new[]
                            {
                                new { kind = "text", text = messageText }
                            }
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{targetAgentUrl}/a2a")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(a2aRequest),
                        Encoding.UTF8,
                        "application/json")
                };

                if (ctx != null)
                {
                    if (!string.IsNullOrEmpty(ctx.SessionId))
                        request.Headers.Add("X-Session-Id", ctx.SessionId);
                    if (!string.IsNullOrEmpty(ctx.CurrentTraceId))
                        request.Headers.Add("X-Trace-Id", ctx.CurrentTraceId);
                    if (!string.IsNullOrEmpty(ctx.UserId))
                        request.Headers.Add("X-User-Id", ctx.UserId);
                }

                var response = await _httpClient.SendAsync(request, ct);

                var responseText = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("A2A call to {Agent} failed with {Status}: {Response}",
                        targetAgentName, response.StatusCode, responseText);
                    var errorResult = $"❌ Błąd komunikacji z agentem (HTTP {(int)response.StatusCode}): {responseText}";
                    _langfuse.EndSpan(span, errorResult, level: "ERROR");
                    await _langfuse.FlushAsync(ct);
                    return errorResult;
                }

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
                    if (result.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var textPart = parts.EnumerateArray()
                            .FirstOrDefault(p => p.TryGetProperty("kind", out var k) && k.GetString() == "text");

                        if (textPart.ValueKind != JsonValueKind.Undefined &&
                            textPart.TryGetProperty("text", out var text))
                        {
                            var resultText = text.GetString() ?? "Operacja zakończona.";
                            _logger.LogInformation("A2A task to {Agent} completed successfully", targetAgentName);
                            _langfuse.EndSpan(span, resultText);
                            await _langfuse.FlushAsync(ct);
                            return resultText;
                        }
                    }

                    // Fallback: return raw result
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
}

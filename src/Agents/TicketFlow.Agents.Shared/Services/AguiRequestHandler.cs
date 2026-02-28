using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TicketFlow.Agents.Shared.Langfuse;

namespace TicketFlow.Agents.Shared.Services;

internal sealed class AguiRequestHandler : IAguiRequestHandler
{
    private readonly ISseWriter _sseWriter;
    private readonly ILogger<AguiRequestHandler> _logger;

    public AguiRequestHandler(ISseWriter sseWriter, ILogger<AguiRequestHandler> logger)
    {
        _sseWriter = sseWriter;
        _logger = logger;
    }

    public async Task HandleRequestAsync(
        HttpContext context,
        IChatClient chatClient,
        AITool[] tools,
        string systemPrompt,
        string requestBody,
        AguiHandlerOptions? options = null)
    {
        options ??= new AguiHandlerOptions();

        using var doc = JsonDocument.Parse(requestBody);
        var messages = new List<ChatMessage>();

        var threadId = doc.RootElement.TryGetProperty("threadId", out var tid)
            ? tid.GetString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();
        var runId = Guid.NewGuid().ToString();

        // For Langfuse
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var parentTraceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault();

        // Ambient trace context for Langfuse
        using var _ = AmbientTraceContext.SetContext(new LangfuseTraceContext
        {
            SessionId = threadId,  // All traces into one conversation
            UserId = userId,
            ParentTraceId = parentTraceId
        });
        using var __ = AmbientAgentProgress.Activate(_sseWriter, context, threadId, runId);

        messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

        if (doc.RootElement.TryGetProperty("messages", out var messagesElement))
        {
            foreach (var msg in messagesElement.EnumerateArray())
            {
                var role = msg.GetProperty("role").GetString();
                var content = msg.TryGetProperty("content", out var c) ? c.GetString() : "";

                var chatRole = role switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    "system" => ChatRole.System,
                    _ => ChatRole.User
                };

                messages.Add(new ChatMessage(chatRole, content ?? ""));
            }
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await _sseWriter.WriteEventAsync(context, new { type = "RUN_STARTED", threadId, runId });

        try
        {
            var messageId = Guid.NewGuid().ToString();
            await _sseWriter.WriteEventAsync(context, new { type = "TEXT_MESSAGE_START", messageId, role = "assistant", threadId, runId });

            var functionInvokingClient = new FunctionInvokingChatClient(chatClient)
            {
                MaximumIterationsPerRequest = options.MaxIterationsPerRequest
            };

            await foreach (var update in functionInvokingClient.GetStreamingResponseAsync(
                messages,
                new ChatOptions { Tools = tools },
                context.RequestAborted))
            {
                if (update.Contents.Any())
                {
                    foreach (var content in update.Contents)
                    {
                        if (content is FunctionCallContent functionCall)
                        {
                            await HandleFunctionCallAsync(context, functionCall, messageId);
                        }
                        else if (content is FunctionResultContent functionResult)
                        {
                            await HandleFunctionResultAsync(context, functionResult);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    await _sseWriter.WriteEventAsync(context, new { type = "TEXT_MESSAGE_CONTENT", messageId, delta = update.Text, threadId, runId });
                }
            }
            
            await _sseWriter.WriteEventAsync(context, new { type = "TEXT_MESSAGE_END", messageId, threadId, runId });
            await _sseWriter.WriteEventAsync(context, new { type = "RUN_FINISHED", threadId, runId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AG-UI request processing");
            try
            {
                await _sseWriter.WriteEventAsync(context, new { type = "RUN_ERROR", message = ex.Message, threadId, runId });
            }
            catch (Exception sseEx)
            {
                _logger.LogError(sseEx, "Failed to send RUN_ERROR event");
            }
        }
    }

    private async Task HandleFunctionCallAsync(HttpContext context, FunctionCallContent functionCall, string messageId)
    {
        var toolCallId = functionCall.CallId ?? Guid.NewGuid().ToString();
        var argsJson = functionCall.Arguments != null
            ? JsonSerializer.Serialize(functionCall.Arguments, new JsonSerializerOptions { WriteIndented = false })
            : "{}";
        
        await _sseWriter.WriteEventAsync(context, new
        {
            type = "TOOL_CALL_START",
            toolCallId,
            toolCallName = functionCall.Name,
            parentMessageId = messageId
        });

        await _sseWriter.WriteEventAsync(context, new
        {
            type = "TOOL_CALL_ARGS",
            toolCallId,
            delta = argsJson
        });

        await _sseWriter.WriteEventAsync(context, new
        {
            type = "TOOL_CALL_END",
            toolCallId
        });
    }

    private async Task HandleFunctionResultAsync(
        HttpContext context,
        FunctionResultContent functionResult)
    {
        var resultText = functionResult.Result?.ToString() ?? "";

        var resultMessageId = Guid.NewGuid().ToString();
        await _sseWriter.WriteEventAsync(context, new
        {
            type = "TOOL_CALL_RESULT",
            messageId = resultMessageId,
            toolCallId = functionResult.CallId,
            content = resultText,
            role = "tool"
        });
    }
}

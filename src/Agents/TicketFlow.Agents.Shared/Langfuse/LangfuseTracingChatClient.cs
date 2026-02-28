using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Agents.Shared.Langfuse;

public class LangfuseTraceContext
{
    public string? SessionId { get; set; }
    public string? ParentTraceId { get; set; }
    public string? UserId { get; set; }
    public string? CurrentTraceId { get; set; }
}

public static class AmbientTraceContext
{
    private static readonly AsyncLocal<LangfuseTraceContext?> _current = new();
    
    public static LangfuseTraceContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static IDisposable SetContext(LangfuseTraceContext context)
    {
        var previous = _current.Value;
        _current.Value = context;
        return new ContextScope(previous);
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly LangfuseTraceContext? _previous;
        public ContextScope(LangfuseTraceContext? previous) => _previous = previous;
        public void Dispose() => _current.Value = _previous;
    }
}

public sealed class LangfuseTracingChatClient : DelegatingChatClient
{
    private readonly ILangfuseClient _langfuse;
    private readonly ILogger<LangfuseTracingChatClient>? _logger;
    private readonly string _agentName;
    private readonly string _model;

    public LangfuseTracingChatClient(
        IChatClient innerClient,
        ILangfuseClient langfuse,
        string agentName,
        string model,
        ILogger<LangfuseTracingChatClient>? logger = null)
        : base(innerClient)
    {
        _langfuse = langfuse;
        _agentName = agentName;
        _model = model;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var userMessage = ExtractLastUserMessage(messageList);

        var ctx = AmbientTraceContext.Current;
        var ownsTrace = string.IsNullOrEmpty(ctx?.CurrentTraceId);

        var trace = ownsTrace
            ? _langfuse.CreateTrace(
                name: $"{_agentName}-llm-call",
                input: userMessage,
                metadata: new Dictionary<string, object>
                {
                    ["model"] = _model,
                    ["toolCount"] = options?.Tools?.Count ?? 0,
                    ["parentTraceId"] = ctx?.ParentTraceId ?? ""
                },
                userId: ctx?.UserId,
                sessionId: ctx?.SessionId)
            : new LangfuseTrace(ctx!.CurrentTraceId!, _agentName);

        if (ownsTrace && ctx != null) ctx.CurrentTraceId = trace.Id;

        var generation = _langfuse.CreateGeneration(
            trace,
            name: "llm-completion",
            model: _model,
            input: userMessage);

        try
        {
            var response = await base.GetResponseAsync(messageList, options, cancellationToken);

            _langfuse.EndGeneration(
                generation,
                output: response.Text,
                inputTokens: response.Usage?.InputTokenCount,
                outputTokens: response.Usage?.OutputTokenCount);

            if (ownsTrace) _langfuse.UpdateTrace(trace, response.Text);
            await _langfuse.FlushAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _langfuse.EndGeneration(generation, $"ERROR: {ex.Message}", level: "ERROR", statusMessage: ex.Message);
            if (ownsTrace) _langfuse.UpdateTrace(trace, $"ERROR: {ex.Message}");
            await _langfuse.FlushAsync(cancellationToken);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var userMessage = ExtractLastUserMessage(messageList);

        var ctx = AmbientTraceContext.Current;
        var ownsTrace = string.IsNullOrEmpty(ctx?.CurrentTraceId);

        var trace = ownsTrace
            ? _langfuse.CreateTrace(
                name: $"{_agentName}-llm-streaming",
                input: userMessage,
                metadata: new Dictionary<string, object>
                {
                    ["model"] = _model,
                    ["streaming"] = true,
                    ["toolCount"] = options?.Tools?.Count ?? 0,
                    ["parentTraceId"] = ctx?.ParentTraceId ?? ""
                },
                userId: ctx?.UserId,
                sessionId: ctx?.SessionId)
            : new LangfuseTrace(ctx!.CurrentTraceId!, _agentName);

        if (ownsTrace && ctx != null) ctx.CurrentTraceId = trace.Id;

        var generation = _langfuse.CreateGeneration(
            trace,
            name: "llm-streaming",
            model: _model,
            input: userMessage);

        var responseBuilder = new StringBuilder();
        var streamCompleted = false;

        try
        {
            await foreach (var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    responseBuilder.Append(update.Text);
                }

                yield return update;
            }
            streamCompleted = true;
        }
        finally
        {
            if (streamCompleted)
            {
                _langfuse.EndGeneration(generation, responseBuilder.ToString());
                if (ownsTrace) _langfuse.UpdateTrace(trace, responseBuilder.ToString());
            }
            else
            {
                _langfuse.EndGeneration(generation, "Stream interrupted", level: "ERROR", statusMessage: "Stream was interrupted or cancelled");
                if (ownsTrace) _langfuse.UpdateTrace(trace, "Stream interrupted");
            }

            _ = _langfuse.FlushAsync(CancellationToken.None);
        }
    }

    private static string ExtractLastUserMessage(IEnumerable<ChatMessage> messages)
    {
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        return lastUserMessage?.Text ?? "";
    }
}

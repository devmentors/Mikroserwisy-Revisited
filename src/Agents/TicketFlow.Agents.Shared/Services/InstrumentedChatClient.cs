using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using TicketFlow.Agents.Shared.Metrics;

namespace TicketFlow.Agents.Shared.Services;

public class InstrumentedChatClient : DelegatingChatClient
{
    private static readonly ActivitySource ActivitySource = new("TicketFlow.Agents.AI");

    private readonly AiMetrics _metrics;
    private readonly string _modelName;
    private readonly string _agentName;

    public InstrumentedChatClient(
        IChatClient innerClient,
        AiMetrics metrics,
        string modelName,
        string agentName)
        : base(innerClient)
    {
        _metrics = metrics;
        _modelName = modelName;
        _agentName = agentName;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ai.inference", ActivityKind.Client);
        activity?.SetTag("ai.model", _modelName);
        activity?.SetTag("ai.agent", _agentName);
        activity?.SetTag("ai.streaming", false);

        var stopwatch = Stopwatch.StartNew();
        _metrics.RecordRequest(_modelName, _agentName);

        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            stopwatch.Stop();

            _metrics.RecordInferenceDuration(stopwatch.Elapsed.TotalMilliseconds, _modelName, _agentName);
            activity?.SetTag("ai.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            
            if (response.Usage != null)
            {
                var inputTokens = response.Usage.InputTokenCount ?? 0;
                var outputTokens = response.Usage.OutputTokenCount ?? 0;
                var totalTokens = response.Usage.TotalTokenCount ?? (inputTokens + outputTokens);

                if (inputTokens > 0)
                    _metrics.RecordTokens(inputTokens, "input", _modelName, _agentName);
                if (outputTokens > 0)
                    _metrics.RecordTokens(outputTokens, "output", _modelName, _agentName);
                
                activity?.SetTag("ai.tokens.input", inputTokens);
                activity?.SetTag("ai.tokens.output", outputTokens);
                activity?.SetTag("ai.tokens.total", totalTokens);
            }
            
            var toolCallCount = 0;
            foreach (var message in response.Messages)
            {
                foreach (var content in message.Contents)
                {
                    if (content is FunctionCallContent functionCall)
                    {
                        _metrics.RecordToolCall(functionCall.Name, _agentName);
                        toolCallCount++;
                    }
                }
            }
            activity?.SetTag("ai.tool_calls", toolCallCount);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordInferenceDuration(stopwatch.Elapsed.TotalMilliseconds, _modelName, _agentName);
            _metrics.RecordError(ex.GetType().Name, _modelName, _agentName);

            activity?.SetTag("ai.error", true);
            activity?.SetTag("ai.error_type", ex.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ai.inference", ActivityKind.Client);
        activity?.SetTag("ai.model", _modelName);
        activity?.SetTag("ai.agent", _agentName);
        activity?.SetTag("ai.streaming", true);
        
        options = EnsureStreamUsageEnabled(options);

        var stopwatch = Stopwatch.StartNew();
        _metrics.RecordRequest(_modelName, _agentName);
        var toolCalls = new HashSet<string>();
        Exception? caughtException = null;
        
        long inputTokens = 0;
        long outputTokens = 0;
        long totalTokens = 0;

        var updates = new List<ChatResponseUpdate>();

        try
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                updates.Add(update);
                
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent functionCall && !string.IsNullOrEmpty(functionCall.Name))
                    {
                        toolCalls.Add(functionCall.Name);
                    }
                    
                    if (content is UsageContent usageContent && usageContent.Details != null)
                    {
                        if (usageContent.Details.InputTokenCount.HasValue)
                            inputTokens = usageContent.Details.InputTokenCount.Value;
                        if (usageContent.Details.OutputTokenCount.HasValue)
                            outputTokens = usageContent.Details.OutputTokenCount.Value;
                        if (usageContent.Details.TotalTokenCount.HasValue)
                            totalTokens = usageContent.Details.TotalTokenCount.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        stopwatch.Stop();
        _metrics.RecordInferenceDuration(stopwatch.Elapsed.TotalMilliseconds, _modelName, _agentName);
        activity?.SetTag("ai.duration_ms", stopwatch.Elapsed.TotalMilliseconds);

        if (caughtException != null)
        {
            _metrics.RecordError(caughtException.GetType().Name, _modelName, _agentName);
            activity?.SetTag("ai.error", true);
            activity?.SetTag("ai.error_type", caughtException.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error, caughtException.Message);
            throw caughtException;
        }
        
        foreach (var toolName in toolCalls)
        {
            _metrics.RecordToolCall(toolName, _agentName);
        }
        activity?.SetTag("ai.tool_calls", toolCalls.Count);
        
        if (inputTokens > 0 || outputTokens > 0)
        {
            _metrics.RecordTokens(inputTokens, "input", _modelName, _agentName);
            _metrics.RecordTokens(outputTokens, "output", _modelName, _agentName);

            activity?.SetTag("ai.tokens.input", inputTokens);
            activity?.SetTag("ai.tokens.output", outputTokens);
            activity?.SetTag("ai.tokens.total", totalTokens > 0 ? totalTokens : inputTokens + outputTokens);
        }
        
        foreach (var update in updates)
        {
            yield return update;
        }
    }

    /// <summary>
    /// Receive token usage data in streaming responses
    /// </summary>
    private static ChatOptions EnsureStreamUsageEnabled(ChatOptions? options)
    {
        options ??= new ChatOptions();
        
        // Supported by OpenAI, Azure OpenAI, and Ollama (via OpenAI-compatible endpoint)
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();

        if (!options.AdditionalProperties.ContainsKey("stream_options"))
        {
            options.AdditionalProperties["stream_options"] = new Dictionary<string, object>
            {
                ["include_usage"] = true
            };
        }

        return options;
    }
}

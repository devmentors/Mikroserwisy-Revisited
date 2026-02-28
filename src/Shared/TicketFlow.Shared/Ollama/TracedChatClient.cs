using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Ollama;

public class TracedChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly ILogger<TracedChatClient> _logger;
    private static readonly ActivitySource ActivitySource = new("TicketFlow.AI");
    private static readonly Meter Meter = new("TicketFlow.AI");

    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>(
        "ai.requests.total",
        description: "Total number of AI requests");

    private static readonly Counter<long> TokensCounter = Meter.CreateCounter<long>(
        "ai.tokens.total",
        description: "Total number of tokens used");

    private static readonly Histogram<double> InferenceHistogram = Meter.CreateHistogram<double>(
        "ai.inference.duration",
        unit: "ms",
        description: "AI inference duration in milliseconds");

    private static readonly Counter<long> ToolCallsCounter = Meter.CreateCounter<long>(
        "ai.tool_calls.total",
        description: "Total number of tool calls");

    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>(
        "ai.errors.total",
        description: "Total number of AI errors");

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public TracedChatClient(IChatClient innerClient, ILogger<TracedChatClient> logger)
    {
        _innerClient = innerClient;
        _logger = logger;
    }

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ai.chat.complete");
        var stopwatch = Stopwatch.StartNew();

        var model = Metadata.ModelId ?? "unknown";
        var promptHash = ComputePromptHash(chatMessages);

        activity?.SetTag("ai.model", model);
        activity?.SetTag("ai.prompt_hash", promptHash);
        activity?.SetTag("ai.tool_count", options?.Tools?.Count ?? 0);

        RequestsCounter.Add(1, new KeyValuePair<string, object?>("model", model));

        try
        {
            var result = await _innerClient.CompleteAsync(chatMessages, options, cancellationToken);

            stopwatch.Stop();

            var inputTokens = result.Usage?.InputTokenCount ?? 0;
            var outputTokens = result.Usage?.OutputTokenCount ?? 0;
            var toolCalls = result.Message.Contents.OfType<FunctionCallContent>().Count();

            activity?.SetTag("ai.input_tokens", inputTokens);
            activity?.SetTag("ai.output_tokens", outputTokens);
            activity?.SetTag("ai.inference_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("ai.tool_calls", toolCalls);

            TokensCounter.Add(inputTokens, new KeyValuePair<string, object?>("type", "input"),
                new KeyValuePair<string, object?>("model", model));
            TokensCounter.Add(outputTokens, new KeyValuePair<string, object?>("type", "output"),
                new KeyValuePair<string, object?>("model", model));
            InferenceHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("model", model));

            if (toolCalls > 0)
            {
                foreach (var fc in result.Message.Contents.OfType<FunctionCallContent>())
                {
                    ToolCallsCounter.Add(1,
                        new KeyValuePair<string, object?>("tool", fc.Name),
                        new KeyValuePair<string, object?>("model", model));
                    activity?.AddEvent(new ActivityEvent("tool_call",
                        tags: new ActivityTagsCollection { { "tool_name", fc.Name } }));
                }
            }

            _logger.LogInformation(
                "AI request completed: model={Model}, tokens={InputTokens}/{OutputTokens}, latency={LatencyMs}ms, tools={ToolCalls}",
                model, inputTokens, outputTokens, stopwatch.ElapsedMilliseconds, toolCalls);

            return result;
        }
        catch (Exception ex)
        {
            ErrorsCounter.Add(1,
                new KeyValuePair<string, object?>("model", model),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name));

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "AI request failed: model={Model}", model);
            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ai.chat.stream");
        var model = Metadata.ModelId ?? "unknown";

        activity?.SetTag("ai.model", model);
        activity?.SetTag("ai.streaming", true);

        RequestsCounter.Add(1,
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("streaming", true));

        await foreach (var update in _innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        return _innerClient.GetService<TService>(key);
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        return _innerClient.GetService(serviceType, key);
    }

    public void Dispose()
    {
        _innerClient.Dispose();
    }

    private static string ComputePromptHash(IList<ChatMessage> messages)
    {
        var combined = string.Join("|", messages.Select(m =>
            $"{m.Role}:{string.Join("", m.Contents.OfType<TextContent>().Select(c => c.Text))}"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}

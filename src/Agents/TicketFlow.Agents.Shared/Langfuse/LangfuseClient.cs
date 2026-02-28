using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Agents.Shared.Configuration;

namespace TicketFlow.Agents.Shared.Langfuse;

public interface ILangfuseClient
{
    bool IsEnabled { get; }
    LangfuseTrace CreateTrace(string name, object? input = null, Dictionary<string, object>? metadata = null, string? userId = null, string? sessionId = null);
    LangfuseGeneration CreateGeneration(LangfuseTrace trace, string name, string model, object? input = null, Dictionary<string, object>? metadata = null);
    LangfuseSpan CreateSpan(LangfuseTrace trace, string name, object? input = null, Dictionary<string, object>? metadata = null);
    void EndGeneration(LangfuseGeneration generation, object? output, long? inputTokens = null, long? outputTokens = null, string? level = null, string? statusMessage = null);
    void EndSpan(LangfuseSpan span, object? output, string? level = null, string? statusMessage = null);
    void UpdateTrace(LangfuseTrace trace, object? output);
    Task FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Ugly glue client, since we use Langfuse 2.x with no OTEL Exporter
/// and only v3.x supports it but needs more than PostgreSQL, so...
/// </summary>
public sealed class LangfuseClient : ILangfuseClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LangfuseClient> _logger;
    private readonly LangfuseOptions _options;
    private readonly List<LangfuseEvent> _pendingEvents = [];
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsEnabled { get; }

    public LangfuseClient(HttpClient httpClient, IOptions<LangfuseOptions> options, ILogger<LangfuseClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        IsEnabled = _options.Enabled && _options.IsConfigured;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        if (IsEnabled)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));
            var authBytes = Encoding.UTF8.GetBytes($"{_options.PublicKey}:{_options.SecretKey}");
            var authHeader = Convert.ToBase64String(authBytes);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        }
        else
        {
            _logger.LogInformation("Langfuse disabled (not configured or disabled in settings)");
        }
    }

    public LangfuseTrace CreateTrace(string name, object? input = null, Dictionary<string, object>? metadata = null, string? userId = null, string? sessionId = null)
    {
        var traceId = Guid.NewGuid().ToString();
        var trace = new LangfuseTrace(traceId, name);

        if (!IsEnabled) return trace;

        var body = new TraceBody
        {
            Id = traceId,
            Name = name,
            Input = input,
            Metadata = metadata,
            UserId = userId,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        QueueEvent("trace-create", body);
        return trace;
    }

    public LangfuseGeneration CreateGeneration(LangfuseTrace trace, string name, string model, object? input = null, Dictionary<string, object>? metadata = null)
    {
        var generationId = Guid.NewGuid().ToString();
        var generation = new LangfuseGeneration(generationId, trace.Id, name, model);

        if (!IsEnabled) return generation;

        var body = new GenerationBody
        {
            Id = generationId,
            TraceId = trace.Id,
            Name = name,
            Model = model,
            Input = input,
            Metadata = metadata,
            StartTime = DateTime.UtcNow.ToString("o")
        };

        QueueEvent("generation-create", body);
        return generation;
    }

    public LangfuseSpan CreateSpan(LangfuseTrace trace, string name, object? input = null, Dictionary<string, object>? metadata = null)
    {
        var spanId = Guid.NewGuid().ToString();
        var span = new LangfuseSpan(spanId, trace.Id, name);

        if (!IsEnabled) return span;

        var body = new SpanBody
        {
            Id = spanId,
            TraceId = trace.Id,
            Name = name,
            Input = input,
            Metadata = metadata,
            StartTime = DateTime.UtcNow.ToString("o")
        };

        QueueEvent("span-create", body);
        return span;
    }

    public void EndGeneration(LangfuseGeneration generation, object? output, long? inputTokens = null, long? outputTokens = null, string? level = null, string? statusMessage = null)
    {
        if (!IsEnabled) return;

        var body = new GenerationBody
        {
            Id = generation.Id,
            TraceId = generation.TraceId,
            Output = output,
            EndTime = DateTime.UtcNow.ToString("o"),
            Level = level,
            StatusMessage = statusMessage,
            Usage = (inputTokens.HasValue || outputTokens.HasValue) ? new UsageData
            {
                Input = inputTokens,
                Output = outputTokens,
                Total = (inputTokens ?? 0) + (outputTokens ?? 0)
            } : null
        };

        QueueEvent("generation-update", body);
    }

    public void EndSpan(LangfuseSpan span, object? output, string? level = null, string? statusMessage = null)
    {
        if (!IsEnabled) return;

        var body = new SpanBody
        {
            Id = span.Id,
            TraceId = span.TraceId,
            Output = output,
            EndTime = DateTime.UtcNow.ToString("o"),
            Level = level,
            StatusMessage = statusMessage
        };

        QueueEvent("span-update", body);
    }

    public void UpdateTrace(LangfuseTrace trace, object? output)
    {
        if (!IsEnabled) return;

        var body = new TraceBody
        {
            Id = trace.Id,
            Output = output
        };

        QueueEvent("trace-create", body);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return;

        List<LangfuseEvent> eventsToSend;
        lock (_lock)
        {
            if (_pendingEvents.Count == 0) return;
            eventsToSend = new List<LangfuseEvent>(_pendingEvents);
            _pendingEvents.Clear();
        }

        try
        {
            var request = new LangfuseBatchRequest { Batch = eventsToSend };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/public/ingestion", content, cancellationToken);

            if (response.IsSuccessStatusCode || (int)response.StatusCode == 207)
            {
                _logger.LogDebug("Flushed {Count} events to Langfuse", eventsToSend.Count);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Langfuse ingestion failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush events to Langfuse");
            lock (_lock)
            {
                _pendingEvents.InsertRange(0, eventsToSend);
            }
        }
    }

    private void QueueEvent(string type, object body)
    {
        var evt = new LangfuseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("o"),
            Type = type,
            Body = body
        };

        lock (_lock)
        {
            _pendingEvents.Add(evt);
        }
    }

    public void Dispose()
    {
        try
        {
            FlushAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore errors during dispose
        }
    }
}

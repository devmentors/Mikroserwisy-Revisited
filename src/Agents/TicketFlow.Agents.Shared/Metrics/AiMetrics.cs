using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TicketFlow.Agents.Shared.Metrics;

public class AiMetrics
{
    public const string MeterName = "TicketFlow.AI";

    private readonly Counter<long> _requestsTotal;
    private readonly Histogram<double> _inferenceDuration;
    private readonly Counter<long> _tokensTotal;
    private readonly Counter<long> _toolCallsTotal;
    private readonly Counter<long> _errorsTotal;
    private readonly Counter<long> _handoversTotal;

    public AiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _requestsTotal = meter.CreateCounter<long>(
            "ai_requests_total",
            description: "Total number of AI inference requests");

        _inferenceDuration = meter.CreateHistogram<double>(
            "ai_inference_duration",
            unit: "ms",
            description: "Duration of AI inference requests in milliseconds");

        _tokensTotal = meter.CreateCounter<long>(
            "ai_tokens_total",
            description: "Total number of tokens processed");

        _toolCallsTotal = meter.CreateCounter<long>(
            "ai_tool_calls_total",
            description: "Total number of tool invocations");

        _errorsTotal = meter.CreateCounter<long>(
            "ai_errors_total",
            description: "Total number of AI errors");

        _handoversTotal = meter.CreateCounter<long>(
            "ai_handovers_total",
            description: "Total number of agent handovers");
    }

    public void RecordRequest(string model, string agent)
    {
        _requestsTotal.Add(1,
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("agent", agent));
    }

    public void RecordInferenceDuration(double durationMs, string model, string agent)
    {
        _inferenceDuration.Record(durationMs,
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("agent", agent));
    }

    public void RecordTokens(long count, string type, string model, string agent)
    {
        _tokensTotal.Add(count,
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("agent", agent));
    }
    
    public void RecordTokenUsage(long inputTokens, long outputTokens, string model, string agent)
    {
        RecordTokens(inputTokens, "input", model, agent);
        RecordTokens(outputTokens, "output", model, agent);
    }
    
    public void RecordToolCall(string toolName, string agent)
    {
        _toolCallsTotal.Add(1,
            new KeyValuePair<string, object?>("tool", toolName),
            new KeyValuePair<string, object?>("agent", agent));
    }
    
    public void RecordError(string errorType, string model, string agent)
    {
        _errorsTotal.Add(1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("agent", agent));
    }
    
    public void RecordHandover(string fromAgent, string toAgent)
    {
        _handoversTotal.Add(1,
            new KeyValuePair<string, object?>("from_agent", fromAgent),
            new KeyValuePair<string, object?>("to_agent", toAgent));
    }
    
    public InferenceMeasurement MeasureInference(string model, string agent)
    {
        return new InferenceMeasurement(this, model, agent);
    }

    public readonly struct InferenceMeasurement : IDisposable
    {
        private readonly AiMetrics _metrics;
        private readonly string _model;
        private readonly string _agent;
        private readonly Stopwatch _stopwatch;

        public InferenceMeasurement(AiMetrics metrics, string model, string agent)
        {
            _metrics = metrics;
            _model = model;
            _agent = agent;
            _stopwatch = Stopwatch.StartNew();
            _metrics.RecordRequest(model, agent);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metrics.RecordInferenceDuration(_stopwatch.Elapsed.TotalMilliseconds, _model, _agent);
        }
    }
}

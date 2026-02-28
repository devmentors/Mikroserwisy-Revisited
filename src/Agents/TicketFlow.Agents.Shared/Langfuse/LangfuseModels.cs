using System.Text.Json.Serialization;

namespace TicketFlow.Agents.Shared.Langfuse;

public class LangfuseBatchRequest
{
    [JsonPropertyName("batch")]
    public List<LangfuseEvent> Batch { get; set; } = [];
}

public class LangfuseEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("type")]
    public string Type { get; set; } = "trace-create";

    [JsonPropertyName("body")]
    public object Body { get; set; } = new();
}

public class TraceBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("input")]
    public object? Input { get; set; }

    [JsonPropertyName("output")]
    public object? Output { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}

public class GenerationBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("input")]
    public object? Input { get; set; }

    [JsonPropertyName("output")]
    public object? Output { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("usage")]
    public UsageData? Usage { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }
}

public class SpanBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("parentObservationId")]
    public string? ParentObservationId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    public object? Input { get; set; }

    [JsonPropertyName("output")]
    public object? Output { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }
}

public class UsageData
{
    [JsonPropertyName("input")]
    public long? Input { get; set; }

    [JsonPropertyName("output")]
    public long? Output { get; set; }

    [JsonPropertyName("total")]
    public long? Total { get; set; }
}

public class LangfuseTrace
{
    public string Id { get; }
    public string Name { get; }
    public DateTime StartTime { get; }

    internal LangfuseTrace(string id, string name)
    {
        Id = id;
        Name = name;
        StartTime = DateTime.UtcNow;
    }
}

public class LangfuseGeneration
{
    public string Id { get; }
    public string TraceId { get; }
    public string Name { get; }
    public string Model { get; }
    public DateTime StartTime { get; }

    internal LangfuseGeneration(string id, string traceId, string name, string model)
    {
        Id = id;
        TraceId = traceId;
        Name = name;
        Model = model;
        StartTime = DateTime.UtcNow;
    }
}

public class LangfuseSpan
{
    public string Id { get; }
    public string TraceId { get; }
    public string Name { get; }
    public DateTime StartTime { get; }

    internal LangfuseSpan(string id, string traceId, string name)
    {
        Id = id;
        TraceId = traceId;
        Name = name;
        StartTime = DateTime.UtcNow;
    }
}

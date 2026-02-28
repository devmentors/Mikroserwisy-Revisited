using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketFlow.Shared.Mcp;

public abstract record SemanticResponse
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Human-readable summary of the result.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Contextual suggestion for the LLM on how to proceed.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Actionable next steps the LLM can take.
    /// </summary>
    public IReadOnlyList<string>? NextSteps { get; init; }

    /// <summary>
    /// Warnings that require attention.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; init; }

    /// <summary>
    /// Serializes the response to JSON for MCP tool output.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, GetType(), SerializerOptions);
}

public sealed record ErrorResponse : SemanticResponse
{
    public required string Error { get; init; }

    public static ErrorResponse Create(string error, string? suggestion = null) => new()
    {
        Summary = error,
        Error = error,
        Suggestion = suggestion
    };
}

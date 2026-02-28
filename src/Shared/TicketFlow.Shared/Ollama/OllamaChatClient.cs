using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TicketFlow.Shared.Ollama;

public class OllamaChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaChatClient> _logger;
    private readonly ActivitySource _activitySource = new("TicketFlow.Ollama");

    public ChatClientMetadata Metadata { get; }

    public OllamaChatClient(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaChatClient> logger)
    {
        _httpClient = httpClient;
        _model = options.Value.GetModel();
        _logger = logger;
        Metadata = new ChatClientMetadata("Ollama", new Uri(options.Value.BaseUrl), _model);
    }

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ollama.chat");
        activity?.SetTag("ollama.model", _model);

        var stopwatch = Stopwatch.StartNew();

        var request = new OllamaChatRequest
        {
            Model = _model,
            Messages = chatMessages.Select(ToOllamaMessage).ToList(),
            Stream = false,
            Tools = options?.Tools?.Select(ToOllamaTool).ToList()
        };

        _logger.LogDebug("Sending chat request to Ollama model {Model}", _model);

        var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);

        stopwatch.Stop();

        activity?.SetTag("ollama.prompt_tokens", ollamaResponse?.PromptEvalCount ?? 0);
        activity?.SetTag("ollama.completion_tokens", ollamaResponse?.EvalCount ?? 0);
        activity?.SetTag("ollama.inference_ms", stopwatch.ElapsedMilliseconds);

        _logger.LogDebug("Ollama response received in {ElapsedMs}ms, tokens: {PromptTokens}/{CompletionTokens}",
            stopwatch.ElapsedMilliseconds,
            ollamaResponse?.PromptEvalCount ?? 0,
            ollamaResponse?.EvalCount ?? 0);

        var chatMessage = new ChatMessage(ChatRole.Assistant, ollamaResponse?.Message?.Content ?? string.Empty);

        if (ollamaResponse?.Message?.ToolCalls?.Any() == true)
        {
            foreach (var toolCall in ollamaResponse.Message.ToolCalls)
            {
                chatMessage.Contents.Add(new FunctionCallContent(
                    toolCall.Id ?? Guid.NewGuid().ToString(),
                    toolCall.Function?.Name ?? "unknown",
                    toolCall.Function?.Arguments ?? new Dictionary<string, object?>()));
            }
        }

        return new ChatCompletion(chatMessage)
        {
            Usage = new UsageDetails
            {
                InputTokenCount = ollamaResponse?.PromptEvalCount,
                OutputTokenCount = ollamaResponse?.EvalCount
            },
            ModelId = _model
        };
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = _model,
            Messages = chatMessages.Select(ToOllamaMessage).ToList(),
            Stream = true,
            Tools = options?.Tools?.Select(ToOllamaTool).ToList()
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message?.Content != null)
            {
                yield return new StreamingChatCompletionUpdate
                {
                    Text = chunk.Message.Content,
                    Role = ChatRole.Assistant
                };
            }
        }
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        if (typeof(TService) == typeof(IChatClient))
            return this as TService;
        return null;
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;
        return null;
    }

    public void Dispose() { }

    private static OllamaMessage ToOllamaMessage(ChatMessage message)
    {
        var role = message.Role.Value switch
        {
            "user" => "user",
            "assistant" => "assistant",
            "system" => "system",
            "tool" => "tool",
            _ => "user"
        };

        var content = string.Join("", message.Contents.OfType<TextContent>().Select(c => c.Text));

        var toolCalls = message.Contents.OfType<FunctionCallContent>()
            .Select(fc => new OllamaToolCall
            {
                Id = fc.CallId,
                Function = new OllamaFunction
                {
                    Name = fc.Name,
                    Arguments = fc.Arguments
                }
            })
            .ToList();

        return new OllamaMessage
        {
            Role = role,
            Content = content,
            ToolCalls = toolCalls.Any() ? toolCalls : null
        };
    }

    private static OllamaTool ToOllamaTool(AITool tool)
    {
        if (tool is not AIFunction function)
            throw new ArgumentException("Only AIFunction tools are supported", nameof(tool));

        return new OllamaTool
        {
            Type = "function",
            Function = new OllamaToolFunction
            {
                Name = function.Metadata.Name,
                Description = function.Metadata.Description ?? "",
                Parameters = new OllamaToolParameters
                {
                    Type = "object",
                    Properties = function.Metadata.Parameters.ToDictionary(
                        p => p.Name,
                        p => new OllamaToolProperty
                        {
                            Type = GetJsonType(p.ParameterType),
                            Description = p.Description ?? ""
                        }),
                    Required = function.Metadata.Parameters
                        .Where(p => p.IsRequired)
                        .Select(p => p.Name)
                        .ToList()
                }
            }
        };
    }

    private static string GetJsonType(Type? type)
    {
        if (type == null) return "string";

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
            TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => "integer",
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
            _ => "string"
        };
    }
}

#region Ollama DTOs

internal class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OllamaTool>? Tools { get; set; }
}

internal class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OllamaToolCall>? ToolCalls { get; set; }
}

internal class OllamaToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("function")]
    public OllamaFunction? Function { get; set; }
}

internal class OllamaFunction
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public IDictionary<string, object?>? Arguments { get; set; }
}

internal class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }
}

internal class OllamaTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OllamaToolFunction? Function { get; set; }
}

internal class OllamaToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("parameters")]
    public OllamaToolParameters? Parameters { get; set; }
}

internal class OllamaToolParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, OllamaToolProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

internal class OllamaToolProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

#endregion

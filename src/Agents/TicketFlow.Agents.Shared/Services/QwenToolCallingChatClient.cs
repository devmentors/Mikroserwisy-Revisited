using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Agents.Shared.Services;

public sealed class QwenToolCallingChatClient : DelegatingChatClient
{
    private readonly ILogger<QwenToolCallingChatClient> _logger;

    public QwenToolCallingChatClient(IChatClient innerClient, ILogger<QwenToolCallingChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fullResponseBuilder = new StringBuilder();
        var tools = options?.Tools?.ToList() ?? new List<AITool>();

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullResponseBuilder.Append(update.Text);
            }
            yield return update;
        }
        
        var fullResponse = fullResponseBuilder.ToString();
        var toolCalls = ExtractToolCalls(fullResponse);

        if (toolCalls.Count > 0 && tools.Count > 0)
        {

            foreach (var (toolName, arguments) in toolCalls)
            {
                var tool = tools.FirstOrDefault(t => t.Name == toolName);
                if (tool == null)
                {
                    _logger.LogWarning("Tool {ToolName} not found in available tools", toolName);
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent($"\n\n❌ Błąd: Narzędzie '{toolName}' nie jest dostępne.")]
                    };
                    continue;
                }
                

                string resultText;
                try
                {
                    var result = await InvokeToolAsync(tool, arguments, cancellationToken);
                    resultText = $"\n\n{result}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking tool {ToolName}", toolName);
                    resultText = $"\n\n❌ Błąd podczas wykonywania narzędzia '{toolName}': {ex.Message}";
                }
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent(resultText)]
                };
            }
        }
    }
    
    private List<(string ToolName, JsonElement Arguments)> ExtractToolCalls(string text)
    {
        var toolCalls = new List<(string, JsonElement)>();

        // Pattern 1: <function=tool_name> ... </tool_call>
        var functionTagPattern = @"<function=(\w+)>";
        var functionMatches = System.Text.RegularExpressions.Regex.Matches(text, functionTagPattern);

        foreach (System.Text.RegularExpressions.Match match in functionMatches)
        {
            try
            {
                var toolName = match.Groups[1].Value;
                var startIndex = match.Index + match.Length;

                // Find the JSON object after the tag
                var jsonStart = text.IndexOf('{', startIndex);
                if (jsonStart == -1) continue;

                // Find matching closing brace
                var braceCount = 0;
                var jsonEnd = -1;
                for (int i = jsonStart; i < text.Length; i++)
                {
                    if (text[i] == '{') braceCount++;
                    else if (text[i] == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            jsonEnd = i;
                            break;
                        }
                    }
                }

                if (jsonEnd == -1) continue;

                var argsJson = text.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(argsJson);
                toolCalls.Add((toolName, doc.RootElement.Clone()));
                _logger.LogInformation("Extracted tool call from <function> tag: {ToolName} with args: {Args}", toolName, argsJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse <function> tag arguments");
            }
        }

        if (toolCalls.Count > 0) return toolCalls;

        // Pattern 2: HeaderCode: TOOLNAME Arguments: {...}
        var headerCodePattern = @"HeaderCode:\s*(\w+)\s+Arguments:\s*(\{[^}]+\})";
        var headerMatches = System.Text.RegularExpressions.Regex.Matches(text, headerCodePattern);

        foreach (System.Text.RegularExpressions.Match match in headerMatches)
        {
            try
            {
                var toolName = match.Groups[1].Value;
                var argsJson = match.Groups[2].Value;

                using var doc = JsonDocument.Parse(argsJson);
                toolCalls.Add((toolName, doc.RootElement.Clone()));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse HeaderCode format arguments: {Args}", match.Groups[2].Value);
            }
        }

        if (toolCalls.Count > 0) return toolCalls;

        // Pattern 3: JSON objects with {"name": "...", "arguments": {...}}
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                int braceCount = 0;
                int endIndex = -1;

                for (int j = i; j < text.Length; j++)
                {
                    if (text[j] == '{') braceCount++;
                    else if (text[j] == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            endIndex = j;
                            break;
                        }
                    }
                }

                if (endIndex > i)
                {
                    var jsonText = text.Substring(i, endIndex - i + 1);
                    try
                    {
                        using var doc = JsonDocument.Parse(jsonText);

                        if (doc.RootElement.TryGetProperty("name", out var nameElement) &&
                            doc.RootElement.TryGetProperty("arguments", out var argsElement))
                        {
                            var toolName = nameElement.GetString();
                            if (!string.IsNullOrEmpty(toolName))
                            {
                                toolCalls.Add((toolName, argsElement.Clone()));
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Not a JSON
                    }

                    // Skip
                    i = endIndex;
                }
            }
        }

        return toolCalls;
    }

    private async Task<object?> InvokeToolAsync(AITool tool, JsonElement arguments, CancellationToken cancellationToken)
    {
        if (tool is not AIFunction aiFunction)
        {
            _logger.LogWarning("Tool {ToolName} is not an AIFunction, cannot invoke", tool.Name);
            return null;
        }

        var argumentsDict = new Dictionary<string, object?>();
        foreach (var property in arguments.EnumerateObject())
        {
            argumentsDict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.ToString()
            };
        }

        // A2A tools are created from DynamicA2AToolsBuilder with Func<Dictionary<string, object>, CancellationToken, Task<string>>
        // They expect a single "args" parameter containing all arguments as a dictionary.
        // We detect A2A tools by their naming pattern (they come from skill IDs with underscores replacing dashes)
        // Common A2A tools: escalate_ticket, delegate_task, etc.
        var isA2ATool = tool.Name.Contains("escalate") || tool.Name.Contains("delegate") || tool.Name.Contains("handover");

        if (isA2ATool && !argumentsDict.ContainsKey("args"))
        {
            // Wrap the arguments in an "args" parameter for A2A tools
            var wrappedArgs = argumentsDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value ?? new object());
            argumentsDict = new Dictionary<string, object?> { ["args"] = wrappedArgs };
            _logger.LogDebug("Wrapped arguments in 'args' for A2A tool {ToolName}", tool.Name);
        }

        var functionArguments = new AIFunctionArguments(argumentsDict);

        var result = await aiFunction.InvokeAsync(functionArguments, cancellationToken);
        return result;
    }
}

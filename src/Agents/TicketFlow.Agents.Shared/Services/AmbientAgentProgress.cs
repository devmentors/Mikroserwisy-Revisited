using Microsoft.AspNetCore.Http;

namespace TicketFlow.Agents.Shared.Services;

public static class AmbientAgentProgress
{
    private static readonly AsyncLocal<Func<string, string, Task>?> _current = new();

    public static Func<string, string, Task>? Current => _current.Value;

    public static IDisposable Activate(ISseWriter sseWriter, HttpContext httpContext, string threadId, string runId)
    {
        var previous = _current.Value;
        _current.Value = async (agentName, text) =>
        {
            var messageId = Guid.NewGuid().ToString();

            await sseWriter.WriteEventAsync(httpContext, new
            {
                type = "TEXT_MESSAGE_START",
                messageId,
                role = "assistant",
                threadId,
                runId
            });

            await sseWriter.WriteEventAsync(httpContext, new
            {
                type = "TEXT_MESSAGE_CONTENT",
                messageId,
                delta = $"🔄 **{agentName}**: {text}\n\n",
                threadId,
                runId
            });

            await sseWriter.WriteEventAsync(httpContext, new
            {
                type = "TEXT_MESSAGE_END",
                messageId,
                threadId,
                runId
            });
        };
        return new ProgressScope(previous);
    }

    private sealed class ProgressScope : IDisposable
    {
        private readonly Func<string, string, Task>? _previous;
        public ProgressScope(Func<string, string, Task>? previous) => _previous = previous;
        public void Dispose() => _current.Value = _previous;
    }
}

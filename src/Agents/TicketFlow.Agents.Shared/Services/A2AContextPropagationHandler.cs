using TicketFlow.Agents.Shared.Langfuse;

namespace TicketFlow.Agents.Shared.Services;

public sealed class A2AContextPropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = AmbientTraceContext.Current;
        if (ctx != null)
        {
            if (!string.IsNullOrEmpty(ctx.SessionId))
                request.Headers.TryAddWithoutValidation("X-Session-Id", ctx.SessionId);
            if (!string.IsNullOrEmpty(ctx.CurrentTraceId))
                request.Headers.TryAddWithoutValidation("X-Trace-Id", ctx.CurrentTraceId);
            if (!string.IsNullOrEmpty(ctx.UserId))
                request.Headers.TryAddWithoutValidation("X-User-Id", ctx.UserId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

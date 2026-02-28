using Microsoft.AspNetCore.Http;

namespace TicketFlow.Agents.Shared.Services;

public interface ISseWriter
{
    Task WriteEventAsync(HttpContext context, object eventData);
}

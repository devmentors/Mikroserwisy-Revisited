using Microsoft.AspNetCore.Http;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Shared.Services;

public interface IUserContextExtractor
{
    UserContext Extract(HttpContext context);
}

using Microsoft.AspNetCore.Http;
using TicketFlow.Shared.Serialization;
namespace TicketFlow.Shared.Exceptions;

internal sealed class ExceptionMiddleware(ISerializer serializer) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (TicketFlowException ex)
        {
            var response = new BadRequestResponse(ex.Message);

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serializer.Serialize(response));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
        }
    }

    public sealed record BadRequestResponse(string Message);
}
namespace TicketFlow.ClientsAPI.Api;

public static class Extensions
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/scalar/v1"));

        app.MapInquiriesV1();
        app.MapInquiriesV2();

        return app;
    }
}

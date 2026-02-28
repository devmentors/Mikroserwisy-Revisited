using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.Translations.Core;
using TicketFlow.Services.Translations.Core.SynchronousIntegration;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCore(builder.Configuration);

var app = builder.Build();

app.UseMetrics();

app.MapGet("/", () => "Translations Service");
app.MapGet("/instance", () => new { Service = "Translations", Instance = Environment.MachineName });

app.MapPost("/translations", async (
    [FromServices] IQueryHandler<GetTranslatedTextSynchronously, string> handler,
    [FromBody] GetTranslatedTextSynchronously query,
    CancellationToken cancellationToken) =>
{
    var translatedText = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(translatedText);
});

app.Run();

public partial class Program { }

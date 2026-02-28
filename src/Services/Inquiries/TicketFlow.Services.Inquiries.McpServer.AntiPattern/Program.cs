using TicketFlow.Services.Inquiries.McpServer.AntiPattern;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration["Postgres:ConnectionString"]
    ?? "Host=localhost;Database=TicketFlow.Inquiries;Username=postgres;Password=";

builder.Services.AddSingleton(new DatabaseSettings(connectionString));

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<QueryInquiriesDirectTool>();

var app = builder.Build();
app.UseCors();
app.MapMcp();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "TicketFlow.Inquiries.McpServer.AntiPattern"
}));

app.Run();

public record DatabaseSettings(string ConnectionString);

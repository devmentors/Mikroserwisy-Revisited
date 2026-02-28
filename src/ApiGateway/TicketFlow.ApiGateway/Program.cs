using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMetrics(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseMetrics();
app.UseCors("AllowAll");

app.MapReverseProxy().RequireCors("AllowAll");

app.Run();

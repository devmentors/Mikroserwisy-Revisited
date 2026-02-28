using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Mock SendGrid API"));
app.MapMockSendGridApi();
app.Run();

public static class MockSendGridEndpoints
{
    private static readonly Random Random = new();
    private static int _requestCount;
    private static DateTime _quotaResetTime = DateTime.UtcNow.AddSeconds(5);
    private const int QuotaLimit = 10;
    private static bool _tokenInvalidated;

    public static void MapMockSendGridApi(this WebApplication app)
    {
        var mail = app.MapGroup("/v3/mail").WithTags("SendGrid API");
        mail.MapPost("/send", SendEmail);

        var admin = app.MapGroup("/admin").WithTags("Demo Control");
        admin.MapGet("/stats", GetStats);
        admin.MapPost("/reset", ResetStats);
        admin.MapPost("/invalidate-token", InvalidateToken);
    }

    private static async Task<IResult> SendEmail([FromBody] SendGridRequest request)
    {
        await Task.Delay(Random.Next(100, 300));

        if (_tokenInvalidated || Random.Next(100) < 10)
            return Results.Json(new { errors = new[] { new { message = "Invalid API key" } } }, statusCode: 401);

        var toEmail = request.Personalizations?.FirstOrDefault()?.To?.FirstOrDefault()?.Email;
        if (string.IsNullOrEmpty(toEmail) || !toEmail.Contains('@'))
            return Results.Json(new { errors = new[] { new { message = "Invalid email" } } }, statusCode: 400);

        if (DateTime.UtcNow >= _quotaResetTime)
        {
            _requestCount = 0;
            _quotaResetTime = DateTime.UtcNow.AddSeconds(5);
        }
        _requestCount++;

        if (_requestCount > QuotaLimit)
            return new QuotaExceededResult((int)(_quotaResetTime - DateTime.UtcNow).TotalSeconds);

        if (Random.Next(100) < 15)
            return Results.Json(new { errors = new[] { new { message = "Service unavailable" } } }, statusCode: 503);

        return Results.Json(new SendGridResponse { MessageId = $"msg_{Guid.NewGuid():N}" });
    }

    private static IResult GetStats() => Results.Json(new
    {
        requestCount = _requestCount,
        quotaLimit = QuotaLimit,
        quotaRemaining = Math.Max(0, QuotaLimit - _requestCount),
        quotaResetInSeconds = (int)Math.Max(0, (_quotaResetTime - DateTime.UtcNow).TotalSeconds),
        tokenValid = !_tokenInvalidated
    });

    private static IResult ResetStats()
    {
        _requestCount = 0;
        _quotaResetTime = DateTime.UtcNow.AddSeconds(5);
        _tokenInvalidated = false;
        return Results.Ok(new { message = "Reset" });
    }

    private static IResult InvalidateToken()
    {
        _tokenInvalidated = true;
        return Results.Ok(new { message = "Token invalidated" });
    }
}

public record SendGridRequest
{
    [JsonPropertyName("personalizations")] public List<Personalization>? Personalizations { get; init; }
    [JsonPropertyName("from")] public EmailAddress? From { get; init; }
    [JsonPropertyName("subject")] public string? Subject { get; init; }
    [JsonPropertyName("content")] public List<Content>? Content { get; init; }
}

public record Personalization { [JsonPropertyName("to")] public List<EmailAddress>? To { get; init; } }
public record EmailAddress { [JsonPropertyName("email")] public string? Email { get; init; } }
public record Content { [JsonPropertyName("value")] public string? Value { get; init; } }
public record SendGridResponse { [JsonPropertyName("message_id")] public string? MessageId { get; init; } }

internal class QuotaExceededResult(int retryAfterSeconds) : IResult
{
    public async Task ExecuteAsync(HttpContext ctx)
    {
        ctx.Response.StatusCode = 429;
        ctx.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        await ctx.Response.WriteAsJsonAsync(new { errors = new[] { new { message = "Quota exceeded" } } });
    }
}

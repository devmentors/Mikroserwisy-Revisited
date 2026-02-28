namespace TicketFlow.Shared.Caching;

public sealed class CacheOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = string.Empty;
    public int DefaultExpirationMinutes { get; set; } = 60;
}

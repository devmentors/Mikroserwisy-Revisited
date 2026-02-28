namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqOptions
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public bool CreateTopology { get; set; }
}
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.RabbitMq;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Testing;

public sealed class MessagingIntegrationTestProvider<TProgram> : IAsyncDisposable where TProgram : class
{
    public RabbitMqContainer RabbitMqContainer { get; }
    public WebApplicationFactory<TProgram> Factory;
    
    private IConnection _rabbitConnection;
    private IModel _rabbitChannel;
    private readonly JsonSerializer _jsonSerializer = new();

    public MessagingIntegrationTestProvider(Action<IServiceCollection> configureServices)
    {
        RabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithName("rabbitmq-integration-test")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithPortBinding(5672, assignRandomHostPort: true)
            .Build();
        
        Factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new[]
                        {
                            new KeyValuePair<string, string>("rabbitMq:hostName", RabbitMqContainer.Hostname),
                            new KeyValuePair<string, string>("rabbitMq:port", RabbitMqContainer.GetMappedPublicPort(5672).ToString()),
                            new KeyValuePair<string, string>("rabbitMq:username", "guest"),
                            new KeyValuePair<string, string>("rabbitMq:password", "guest"),
                            new KeyValuePair<string, string>("rabbitMq:createTopology", "false")
                        }!);
                    })
                    .ConfigureServices(services => configureServices(services));
            });
    }

    public void Initialize(Action<IModel> provisionTopology)
    {
        var factory = new ConnectionFactory
        {
            HostName = RabbitMqContainer.Hostname,
            Port = RabbitMqContainer.GetMappedPublicPort(5672)
        };

        _rabbitConnection = factory.CreateConnection();
        _rabbitChannel = _rabbitConnection.CreateModel();

        provisionTopology(_rabbitChannel);
    }

    public void Publish<TMessage>(TMessage message, string exchange, string routingKey) where TMessage : class, IMessage
    {
        var payload = _jsonSerializer.SerializeBinary(message);
        _rabbitChannel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: null, body: payload);
    }
    
    public async Task<TMessage> ConsumeMessagesAsync<TMessage>(string queueName, int maxDelay = 1_000) where TMessage : IMessage
    {
        var taskCompletionSource = new TaskCompletionSource<TMessage>();
        var consumer = new EventingBasicConsumer(_rabbitChannel);

        consumer.Received += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = _jsonSerializer.DeserializeBinary<TMessage>(body);
            taskCompletionSource.SetResult(message);
        };

        _rabbitChannel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(maxDelay).ContinueWith(_ => taskCompletionSource.SetResult(default)));
        return taskCompletionSource.Task.Result;
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await CastAndDispose(_rabbitConnection);
        await CastAndDispose(_rabbitChannel);
        await RabbitMqContainer.DisposeAsync();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}
using System.Text;
using System.Text.Json;
using MessagingContracts;
using RabbitMQ.Client;

namespace InventoryService.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishAsync<T>(
        T message,
        string routingKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: RabbitMqNames.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.CorrelationId = correlationId;
        properties.MessageId = Guid.NewGuid().ToString();

        channel.BasicPublish(
            exchange: RabbitMqNames.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published message {MessageType} with RoutingKey {RoutingKey}, CorrelationId {CorrelationId}",
            typeof(T).Name,
            routingKey,
            correlationId);

        return Task.CompletedTask;
    }
}
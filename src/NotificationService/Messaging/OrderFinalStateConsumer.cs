using System.Text;
using System.Text.Json;
using MessagingContracts;
using NotificationService.DAL;
using NotificationService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Messaging;

public class OrderFinalStateConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderFinalStateConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderFinalStateConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrderFinalStateConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: RabbitMqNames.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        DeclareAndBindQueue(
            RabbitMqNames.NotificationOrderConfirmedQueue,
            RabbitMqNames.OrderConfirmedRoutingKey);

        DeclareAndBindQueue(
            RabbitMqNames.NotificationOrderRejectedQueue,
            RabbitMqNames.OrderRejectedRoutingKey);

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        StartConsumer(
            RabbitMqNames.NotificationOrderConfirmedQueue,
            HandleOrderConfirmedAsync,
            stoppingToken);

        StartConsumer(
            RabbitMqNames.NotificationOrderRejectedQueue,
            HandleOrderRejectedAsync,
            stoppingToken);

        _logger.LogInformation("NotificationService OrderFinalStateConsumer started.");

        return Task.CompletedTask;
    }

    private void DeclareAndBindQueue(string queueName, string routingKey)
    {
        _channel!.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: queueName,
            exchange: RabbitMqNames.ExchangeName,
            routingKey: routingKey);
    }

    private void StartConsumer(
        string queueName,
        Func<string, string, CancellationToken, Task> handler,
        CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            var correlationId = eventArgs.BasicProperties?.CorrelationId
                ?? Guid.NewGuid().ToString();

            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                await handler(json, correlationId, stoppingToken);

                _channel!.BasicAck(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process order final state message. Queue {QueueName}, CorrelationId {CorrelationId}",
                    queueName,
                    correlationId);

                _channel!.BasicNack(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        _channel!.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task HandleOrderConfirmedAsync(
        string json,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<OrderConfirmedMessage>(json);

        if (message == null)
        {
            _logger.LogWarning(
                "Invalid OrderConfirmed message. CorrelationId {CorrelationId}",
                correlationId);

            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = new NotificationMessage
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            Status = "Confirmed",
            Message = message.Message,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification saved for confirmed OrderId {OrderId}. CorrelationId {CorrelationId}",
            message.OrderId,
            correlationId);
    }

    private async Task HandleOrderRejectedAsync(
        string json,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<OrderRejectedMessage>(json);

        if (message == null)
        {
            _logger.LogWarning(
                "Invalid OrderRejected message. CorrelationId {CorrelationId}",
                correlationId);

            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = new NotificationMessage
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            Status = "Rejected",
            Message = string.IsNullOrWhiteSpace(message.Reason)
                ? "Order was rejected."
                : message.Reason,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Notification saved for rejected OrderId {OrderId}. Reason: {Reason}. CorrelationId {CorrelationId}",
            message.OrderId,
            message.Reason,
            correlationId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
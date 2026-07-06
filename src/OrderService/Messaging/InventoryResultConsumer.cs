using System.Text;
using System.Text.Json;
using MessagingContracts;
using Microsoft.EntityFrameworkCore;
using OrderService.DAL;
using OrderService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Messaging;

public class InventoryResultConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InventoryResultConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public InventoryResultConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<InventoryResultConsumer> logger)
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
            RabbitMqNames.OrderInventoryReservedQueue,
            RabbitMqNames.InventoryReservedRoutingKey);

        DeclareAndBindQueue(
            RabbitMqNames.OrderInventoryRejectedQueue,
            RabbitMqNames.InventoryRejectedRoutingKey);

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        StartConsumer(
            RabbitMqNames.OrderInventoryReservedQueue,
            HandleInventoryReservedAsync,
            stoppingToken);

        StartConsumer(
            RabbitMqNames.OrderInventoryRejectedQueue,
            HandleInventoryRejectedAsync,
            stoppingToken);

        _logger.LogInformation("OrderService InventoryResultConsumer started.");

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
                    "Failed to process inventory result message. Queue {QueueName}, CorrelationId {CorrelationId}",
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

    private async Task HandleInventoryReservedAsync(
        string json,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<InventoryReservedMessage>(json);

        if (message == null)
        {
            _logger.LogWarning(
                "Invalid InventoryReserved message. CorrelationId {CorrelationId}",
                correlationId);

            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning(
                "Order {OrderId} was not found while handling InventoryReserved. CorrelationId {CorrelationId}",
                message.OrderId,
                correlationId);

            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation(
                "Order {OrderId} already processed with status {Status}. Skipping duplicate InventoryReserved. CorrelationId {CorrelationId}",
                order.Id,
                order.Status,
                correlationId);

            return;
        }

        order.Status = OrderStatus.Confirmed;

        await dbContext.SaveChangesAsync(cancellationToken);

        var orderConfirmedMessage = new OrderConfirmedMessage
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            CorrelationId = correlationId,
            Message = "Order confirmed successfully."
        };

        await publisher.PublishAsync(
            orderConfirmedMessage,
            RabbitMqNames.OrderConfirmedRoutingKey,
            correlationId,
            cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} confirmed. CorrelationId {CorrelationId}",
            order.Id,
            correlationId);
    }

    private async Task HandleInventoryRejectedAsync(
        string json,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<InventoryRejectedMessage>(json);

        if (message == null)
        {
            _logger.LogWarning(
                "Invalid InventoryRejected message. CorrelationId {CorrelationId}",
                correlationId);

            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning(
                "Order {OrderId} was not found while handling InventoryRejected. CorrelationId {CorrelationId}",
                message.OrderId,
                correlationId);

            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation(
                "Order {OrderId} already processed with status {Status}. Skipping duplicate InventoryRejected. CorrelationId {CorrelationId}",
                order.Id,
                order.Status,
                correlationId);

            return;
        }

        order.Status = OrderStatus.Rejected;

        await dbContext.SaveChangesAsync(cancellationToken);

        var orderRejectedMessage = new OrderRejectedMessage
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            CorrelationId = correlationId,
            Reason = message.Reason
        };

        await publisher.PublishAsync(
            orderRejectedMessage,
            RabbitMqNames.OrderRejectedRoutingKey,
            correlationId,
            cancellationToken);

        _logger.LogWarning(
            "Order {OrderId} rejected. Reason: {Reason}. CorrelationId {CorrelationId}",
            order.Id,
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
using System.Text;
using System.Text.Json;
using InventoryService.DAL;
using MessagingContracts;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Messaging;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderPlacedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrderPlacedConsumer> logger)
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

        _channel.QueueDeclare(
            queue: RabbitMqNames.InventoryOrderPlacedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: RabbitMqNames.InventoryOrderPlacedQueue,
            exchange: RabbitMqNames.ExchangeName,
            routingKey: RabbitMqNames.OrderPlacedRoutingKey);

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            var correlationId = eventArgs.BasicProperties?.CorrelationId
                ?? Guid.NewGuid().ToString();

            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var message = JsonSerializer.Deserialize<OrderPlacedMessage>(json);

                if (message == null)
                {
                    _logger.LogWarning(
                        "Received invalid OrderPlaced message. CorrelationId {CorrelationId}",
                        correlationId);

                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Received OrderPlaced for OrderId {OrderId}, CorrelationId {CorrelationId}",
                    message.OrderId,
                    correlationId);

                await HandleOrderPlacedAsync(message, correlationId, stoppingToken);

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process OrderPlaced message. CorrelationId {CorrelationId}",
                    correlationId);

                _channel.BasicNack(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: RabbitMqNames.InventoryOrderPlacedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Inventory OrderPlacedConsumer started.");

        return Task.CompletedTask;
    }

    private async Task HandleOrderPlacedAsync(
        OrderPlacedMessage message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        foreach (var item in message.Items)
        {
            if (item.Quantity <= 0)
            {
                await PublishRejectedAsync(
                    publisher,
                    message,
                    correlationId,
                    $"Invalid quantity for product {item.ProductId}.",
                    cancellationToken);

                return;
            }

            var inventoryItem = await dbContext.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId, cancellationToken);

            if (inventoryItem == null)
            {
                await PublishRejectedAsync(
                    publisher,
                    message,
                    correlationId,
                    $"Inventory item for product {item.ProductId} was not found.",
                    cancellationToken);

                return;
            }

            if (inventoryItem.QuantityAvailable < item.Quantity)
            {
                await PublishRejectedAsync(
                    publisher,
                    message,
                    correlationId,
                    $"Not enough stock for product {item.ProductId}. Available: {inventoryItem.QuantityAvailable}, Requested: {item.Quantity}.",
                    cancellationToken);

                return;
            }
        }

        foreach (var item in message.Items)
        {
            var inventoryItem = await dbContext.InventoryItems
                .FirstAsync(i => i.ProductId == item.ProductId, cancellationToken);

            inventoryItem.QuantityAvailable -= item.Quantity;
            inventoryItem.QuantityReserved += item.Quantity;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var reservedMessage = new InventoryReservedMessage
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            CorrelationId = correlationId,
            Message = "Inventory reserved successfully."
        };

        await publisher.PublishAsync(
            reservedMessage,
            RabbitMqNames.InventoryReservedRoutingKey,
            correlationId,
            cancellationToken);

        _logger.LogInformation(
            "Inventory reserved for OrderId {OrderId}, CorrelationId {CorrelationId}",
            message.OrderId,
            correlationId);
    }

    private async Task PublishRejectedAsync(
        IRabbitMqPublisher publisher,
        OrderPlacedMessage message,
        string correlationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var rejectedMessage = new InventoryRejectedMessage
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            CorrelationId = correlationId,
            Reason = reason
        };

        await publisher.PublishAsync(
            rejectedMessage,
            RabbitMqNames.InventoryRejectedRoutingKey,
            correlationId,
            cancellationToken);

        _logger.LogWarning(
            "Inventory rejected for OrderId {OrderId}. Reason: {Reason}. CorrelationId {CorrelationId}",
            message.OrderId,
            reason,
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
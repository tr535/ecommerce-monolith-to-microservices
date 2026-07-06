using System.Text;
using System.Text.Json;
using InventoryService.DAL;
using InventoryService.Models;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectToRabbitMqWithRetryAsync(stoppingToken);

        if (_channel == null)
        {
            _logger.LogError("RabbitMQ channel was not created. Inventory consumer will not start.");
            return;
        }

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

                    _channel.BasicAck(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false);

                    return;
                }

                _logger.LogInformation(
                    "Received OrderPlaced for OrderId {OrderId}. CorrelationId {CorrelationId}",
                    message.OrderId,
                    correlationId);

                await HandleOrderPlacedAsync(message, correlationId, stoppingToken);

                _channel.BasicAck(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process OrderPlaced message. CorrelationId {CorrelationId}",
                    correlationId);

                if (_channel?.IsOpen == true)
                {
                    _channel.BasicNack(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            }
        };

        _channel.BasicConsume(
            queue: RabbitMqNames.InventoryOrderPlacedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Inventory OrderPlacedConsumer started.");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Inventory OrderPlacedConsumer is stopping.");
        }
    }

    private async Task ConnectToRabbitMqWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _logger.LogInformation("InventoryService connected to RabbitMQ.");

                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ is not ready yet. InventoryService will retry in 5 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleOrderPlacedAsync(
        OrderPlacedMessage message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        var existingReservation = await dbContext.InventoryReservations
            .FirstOrDefaultAsync(r => r.OrderId == message.OrderId, cancellationToken);

        if (existingReservation != null)
        {
            _logger.LogInformation(
                "OrderId {OrderId} was already processed by InventoryService with status {Status}. Re-publishing previous result. CorrelationId {CorrelationId}",
                message.OrderId,
                existingReservation.Status,
                correlationId);

            if (existingReservation.Status == "Reserved")
            {
                var reservedMessage = new InventoryReservedMessage
                {
                    OrderId = message.OrderId,
                    CustomerEmail = existingReservation.CustomerEmail,
                    CorrelationId = existingReservation.CorrelationId,
                    Message = "Inventory was already reserved for this order."
                };

                await publisher.PublishAsync(
                    reservedMessage,
                    RabbitMqNames.InventoryReservedRoutingKey,
                    existingReservation.CorrelationId,
                    cancellationToken);
            }
            else
            {
                var rejectedMessage = new InventoryRejectedMessage
                {
                    OrderId = message.OrderId,
                    CustomerEmail = existingReservation.CustomerEmail,
                    CorrelationId = existingReservation.CorrelationId,
                    Reason = existingReservation.Reason ?? "Inventory was already rejected for this order."
                };

                await publisher.PublishAsync(
                    rejectedMessage,
                    RabbitMqNames.InventoryRejectedRoutingKey,
                    existingReservation.CorrelationId,
                    cancellationToken);
            }

            return;
        }

        foreach (var item in message.Items)
        {
            if (item.Quantity <= 0)
            {
                await SaveRejectedReservationAndPublishAsync(
                    dbContext,
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
                await SaveRejectedReservationAndPublishAsync(
                    dbContext,
                    publisher,
                    message,
                    correlationId,
                    $"Inventory item for product {item.ProductId} was not found.",
                    cancellationToken);

                return;
            }

            if (inventoryItem.QuantityAvailable < item.Quantity)
            {
                await SaveRejectedReservationAndPublishAsync(
                    dbContext,
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

        var reservation = new InventoryReservation
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            CorrelationId = correlationId,
            Status = "Reserved",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.InventoryReservations.Add(reservation);

        await dbContext.SaveChangesAsync(cancellationToken);

        var reservedMessageToPublish = new InventoryReservedMessage
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            CorrelationId = correlationId,
            Message = "Inventory reserved successfully."
        };

        await publisher.PublishAsync(
            reservedMessageToPublish,
            RabbitMqNames.InventoryReservedRoutingKey,
            correlationId,
            cancellationToken);

        _logger.LogInformation(
            "Inventory reserved for OrderId {OrderId}. CorrelationId {CorrelationId}",
            message.OrderId,
            correlationId);
    }

    private async Task SaveRejectedReservationAndPublishAsync(
        InventoryDbContext dbContext,
        IRabbitMqPublisher publisher,
        OrderPlacedMessage message,
        string correlationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var reservation = new InventoryReservation
        {
            OrderId = message.OrderId,
            CustomerEmail = message.CustomerEmail,
            CorrelationId = correlationId,
            Status = "Rejected",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.InventoryReservations.Add(reservation);

        await dbContext.SaveChangesAsync(cancellationToken);

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
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch
        {
            // Ignore dispose errors during shutdown.
        }

        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
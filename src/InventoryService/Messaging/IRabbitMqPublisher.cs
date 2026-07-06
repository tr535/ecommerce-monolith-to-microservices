namespace InventoryService.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(
        T message,
        string routingKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
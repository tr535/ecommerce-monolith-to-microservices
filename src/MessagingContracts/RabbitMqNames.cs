namespace MessagingContracts;

public static class RabbitMqNames
{
    public const string ExchangeName = "ecommerce.saga";

    public const string OrderPlacedRoutingKey = "order.placed";
    public const string InventoryReservedRoutingKey = "inventory.reserved";
    public const string InventoryRejectedRoutingKey = "inventory.rejected";
    public const string OrderConfirmedRoutingKey = "order.confirmed";
    public const string OrderRejectedRoutingKey = "order.rejected";

    public const string InventoryOrderPlacedQueue = "inventory.order-placed";

    public const string OrderInventoryReservedQueue = "order.inventory-reserved";
    public const string OrderInventoryRejectedQueue = "order.inventory-rejected";

    public const string NotificationOrderConfirmedQueue = "notification.order-confirmed";
    public const string NotificationOrderRejectedQueue = "notification.order-rejected";
}
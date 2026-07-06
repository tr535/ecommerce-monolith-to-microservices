namespace MessagingContracts;

public class OrderPlacedMessage
{
    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public List<OrderPlacedItemMessage> Items { get; set; } = new();
}

public class OrderPlacedItemMessage
{
    public string ProductId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
namespace MessagingContracts;

public class OrderConfirmedMessage
{
    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string Message { get; set; } = "Order confirmed successfully.";
}
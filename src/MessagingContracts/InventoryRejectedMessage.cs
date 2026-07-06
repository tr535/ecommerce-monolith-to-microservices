namespace MessagingContracts;

public class InventoryRejectedMessage
{
    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
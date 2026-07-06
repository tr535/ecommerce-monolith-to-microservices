namespace InventoryService.Models;

public class InventoryReservation
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    // Reserved / Rejected

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
namespace InventoryService.Models;

public class InventoryItem
{
    public int Id { get; set; }

    public string ProductId { get; set; } = string.Empty;

    public int QuantityAvailable { get; set; }

    public int QuantityReserved { get; set; }
}
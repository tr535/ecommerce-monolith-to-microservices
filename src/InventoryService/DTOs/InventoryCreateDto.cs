namespace InventoryService.DTOs;

public class InventoryCreateDto
{
    public string ProductId { get; set; } = string.Empty;

    public int QuantityAvailable { get; set; }

    public int QuantityReserved { get; set; }
}
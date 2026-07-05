namespace InventoryService.DTOs;

public class ReserveInventoryRequestDto
{
    public string ProductId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
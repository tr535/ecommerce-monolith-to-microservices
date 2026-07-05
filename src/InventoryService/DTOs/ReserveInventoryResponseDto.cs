namespace InventoryService.DTOs;

public class ReserveInventoryResponseDto
{
    public string ProductId { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int QuantityAvailable { get; set; }

    public int QuantityReserved { get; set; }
}
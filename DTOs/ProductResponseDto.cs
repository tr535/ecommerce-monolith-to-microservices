namespace EcommerceMonolith.DTOs;

public class ProductResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public int QuantityAvailable { get; set; }

    public int QuantityReserved { get; set; }
}
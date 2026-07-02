namespace EcommerceMonolith.DTOs;

public class OrderCreateDto
{
    public string CustomerEmail { get; set; } = string.Empty;

    public List<OrderItemCreateDto> Items { get; set; } = new();
}
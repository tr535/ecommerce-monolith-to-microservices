namespace OrderService.DTOs;

public class OrderItemCreateDto
{
    public string ProductId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
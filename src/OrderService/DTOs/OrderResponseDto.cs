namespace OrderService.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = new();
}
using EcommerceMonolith.Models;

namespace EcommerceMonolith.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = new();
}
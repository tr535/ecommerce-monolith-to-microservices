namespace EcommerceMonolith.Models;

public class Order
{
    public int Id { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
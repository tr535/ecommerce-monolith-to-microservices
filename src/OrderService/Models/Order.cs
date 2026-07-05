namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
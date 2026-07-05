namespace NotificationService.Models;

public class NotificationMessage
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
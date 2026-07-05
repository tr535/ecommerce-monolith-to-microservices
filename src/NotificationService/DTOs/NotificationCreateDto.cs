namespace NotificationService.DTOs;

public class NotificationCreateDto
{
    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
using System.Net.Http.Json;

namespace OrderService.Clients;

public class NotificationClient
{
    private readonly HttpClient _httpClient;

    public NotificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendNotificationAsync(CreateNotificationRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Notifications", request);

        response.EnsureSuccessStatusCode();
    }
}

public class CreateNotificationRequestDto
{
    public int OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
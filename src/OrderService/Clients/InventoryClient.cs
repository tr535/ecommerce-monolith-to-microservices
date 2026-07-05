using System.Net.Http.Json;

namespace OrderService.Clients;

public class InventoryClient
{
    private readonly HttpClient _httpClient;

    public InventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReserveInventoryResponseDto?> ReserveAsync(ReserveInventoryRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", request);

        return await response.Content.ReadFromJsonAsync<ReserveInventoryResponseDto>();
    }
}

public class ReserveInventoryRequestDto
{
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; }
}

public class ReserveInventoryResponseDto
{
    public bool Success { get; set; }
}
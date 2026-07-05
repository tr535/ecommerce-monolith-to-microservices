using System.Net.Http.Json;

namespace OrderService.Clients;

public class ProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public ProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductCatalogProductDto?> GetProductByIdAsync(string productId)
    {
        return await _httpClient.GetFromJsonAsync<ProductCatalogProductDto>(
            $"/api/Products/{productId}");
    }
}

public class ProductCatalogProductDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
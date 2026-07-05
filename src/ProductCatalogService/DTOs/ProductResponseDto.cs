namespace ProductCatalogService.DTOs;

public class ProductResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public Dictionary<string, string> Attributes { get; set; } = new();
}
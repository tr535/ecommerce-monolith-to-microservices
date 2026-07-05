using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductCatalogService.DTOs;
using ProductCatalogService.Models;

namespace ProductCatalogService.Services;

public class ProductService : IProductService
{
    private readonly IMongoCollection<Product> _products;

    public ProductService(IOptions<MongoDbSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);

        _products = mongoDatabase.GetCollection<Product>(
            settings.Value.ProductsCollectionName);
    }

    public async Task<List<ProductResponseDto>> GetAllAsync()
    {
        var products = await _products.Find(_ => true).ToListAsync();

        return products.Select(MapToResponseDto).ToList();
    }

    public async Task<ProductResponseDto?> GetByIdAsync(string id)
    {
        var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();

        if (product == null)
        {
            return null;
        }

        return MapToResponseDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto productDto)
    {
        var product = new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            Category = productDto.Category,
            Attributes = productDto.Attributes
        };

        await _products.InsertOneAsync(product);

        return MapToResponseDto(product);
    }

    private static ProductResponseDto MapToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id ?? string.Empty,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            Attributes = product.Attributes
        };
    }
}
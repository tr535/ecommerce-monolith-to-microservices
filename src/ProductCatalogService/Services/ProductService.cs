using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductCatalogService.DTOs;
using ProductCatalogService.Models;
using StackExchange.Redis;

namespace ProductCatalogService.Services;

public class ProductService : IProductService
{
    private const string AllProductsCacheKey = "products:all";
    private const int CacheExpirationMinutes = 5;

    private readonly IMongoCollection<Product> _products;
    private readonly IDatabase _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IOptions<MongoDbSettings> settings,
        IConnectionMultiplexer redis,
        ILogger<ProductService> logger)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);

        _products = mongoDatabase.GetCollection<Product>(
            settings.Value.ProductsCollectionName);

        _cache = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<List<ProductResponseDto>> GetAllAsync()
    {
        var cachedProducts = await _cache.StringGetAsync(AllProductsCacheKey);

        if (cachedProducts.HasValue)
        {
            _logger.LogInformation("CACHE HIT: {CacheKey}", AllProductsCacheKey);

            var productsFromCache = JsonSerializer.Deserialize<List<ProductResponseDto>>(
                cachedProducts!);

            return productsFromCache ?? new List<ProductResponseDto>();
        }

        _logger.LogInformation("CACHE MISS: {CacheKey}. Loading products from MongoDB.", AllProductsCacheKey);

        var products = await _products.Find(_ => true).ToListAsync();

        var productDtos = products
            .Select(MapToResponseDto)
            .ToList();

        await _cache.StringSetAsync(
            AllProductsCacheKey,
            JsonSerializer.Serialize(productDtos),
            TimeSpan.FromMinutes(CacheExpirationMinutes));

        return productDtos;
    }

    public async Task<ProductResponseDto?> GetByIdAsync(string id)
    {
        var cacheKey = GetProductByIdCacheKey(id);

        var cachedProduct = await _cache.StringGetAsync(cacheKey);

        if (cachedProduct.HasValue)
        {
            _logger.LogInformation("CACHE HIT: {CacheKey}", cacheKey);

            return JsonSerializer.Deserialize<ProductResponseDto>(cachedProduct!);
        }

        _logger.LogInformation("CACHE MISS: {CacheKey}. Loading product from MongoDB.", cacheKey);

        var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();

        if (product == null)
        {
            return null;
        }

        var productDto = MapToResponseDto(product);

        await _cache.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(productDto),
            TimeSpan.FromMinutes(CacheExpirationMinutes));

        return productDto;
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

        var response = MapToResponseDto(product);

        await InvalidateProductCacheAsync(response.Id);

        _logger.LogInformation(
            "Product {ProductId} created. Product catalog cache invalidated.",
            response.Id);

        return response;
    }

    private async Task InvalidateProductCacheAsync(string productId)
    {
        await _cache.KeyDeleteAsync(AllProductsCacheKey);

        if (!string.IsNullOrWhiteSpace(productId))
        {
            await _cache.KeyDeleteAsync(GetProductByIdCacheKey(productId));
        }

        _logger.LogInformation("CACHE INVALIDATED: product catalog cache was cleared.");
    }

    private static string GetProductByIdCacheKey(string id)
    {
        return $"products:id:{id}";
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
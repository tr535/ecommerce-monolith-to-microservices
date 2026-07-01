using EcommerceMonolith.DAL;
using EcommerceMonolith.DTOs;
using EcommerceMonolith.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceMonolith.BLL;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponseDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .Include(p => p.InventoryItem)
            .ToListAsync();

        return products.Select(MapToResponseDto).ToList();
    }

    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        return MapToResponseDto(product);
    }

    public async Task<ProductResponseDto> CreateProductAsync(ProductCreateDto productDto)
    {
        var product = MapToEntity(productDto);

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponseDto(product);
    }

    private static Product MapToEntity(ProductCreateDto productDto)
    {
        return new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            Category = productDto.Category,
            InventoryItem = new InventoryItem
            {
                QuantityAvailable = productDto.QuantityAvailable,
                QuantityReserved = productDto.QuantityReserved
            }
        };
    }

    private static ProductResponseDto MapToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            QuantityAvailable = product.InventoryItem?.QuantityAvailable ?? 0,
            QuantityReserved = product.InventoryItem?.QuantityReserved ?? 0
        };
    }
}
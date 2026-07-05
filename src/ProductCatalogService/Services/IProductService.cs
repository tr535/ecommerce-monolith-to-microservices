using ProductCatalogService.DTOs;

namespace ProductCatalogService.Services;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllAsync();

    Task<ProductResponseDto?> GetByIdAsync(string id);

    Task<ProductResponseDto> CreateAsync(ProductCreateDto productDto);
}
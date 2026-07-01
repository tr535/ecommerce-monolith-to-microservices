using EcommerceMonolith.DTOs;

namespace EcommerceMonolith.BLL;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllProductsAsync();

    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    Task<ProductResponseDto> CreateProductAsync(ProductCreateDto productDto);
}
using InventoryService.DTOs;

namespace InventoryService.Services;

public interface IInventoryService
{
    Task<List<InventoryResponseDto>> GetAllAsync();

    Task<InventoryResponseDto?> GetByProductIdAsync(string productId);

    Task<InventoryResponseDto> CreateAsync(InventoryCreateDto inventoryDto);

    Task<ReserveInventoryResponseDto> ReserveAsync(ReserveInventoryRequestDto request);
}
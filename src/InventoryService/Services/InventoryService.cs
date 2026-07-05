using InventoryService.DAL;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext _context;

    public InventoryService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryResponseDto>> GetAllAsync()
    {
        var items = await _context.InventoryItems.ToListAsync();

        return items.Select(MapToResponseDto).ToList();
    }

    public async Task<InventoryResponseDto?> GetByProductIdAsync(string productId)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        if (item == null)
        {
            return null;
        }

        return MapToResponseDto(item);
    }

    public async Task<InventoryResponseDto> CreateAsync(InventoryCreateDto inventoryDto)
    {
        var item = new InventoryItem
        {
            ProductId = inventoryDto.ProductId,
            QuantityAvailable = inventoryDto.QuantityAvailable,
            QuantityReserved = inventoryDto.QuantityReserved
        };

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return MapToResponseDto(item);
    }

    public async Task<ReserveInventoryResponseDto> ReserveAsync(ReserveInventoryRequestDto request)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId);

        if (item == null)
        {
            return new ReserveInventoryResponseDto
            {
                ProductId = request.ProductId,
                Success = false,
                Message = "Inventory item was not found.",
                QuantityAvailable = 0,
                QuantityReserved = 0
            };
        }

        if (request.Quantity <= 0)
        {
            return new ReserveInventoryResponseDto
            {
                ProductId = request.ProductId,
                Success = false,
                Message = "Quantity must be greater than zero.",
                QuantityAvailable = item.QuantityAvailable,
                QuantityReserved = item.QuantityReserved
            };
        }

        if (item.QuantityAvailable < request.Quantity)
        {
            return new ReserveInventoryResponseDto
            {
                ProductId = request.ProductId,
                Success = false,
                Message = "Not enough inventory available.",
                QuantityAvailable = item.QuantityAvailable,
                QuantityReserved = item.QuantityReserved
            };
        }

        item.QuantityAvailable -= request.Quantity;
        item.QuantityReserved += request.Quantity;

        await _context.SaveChangesAsync();

        return new ReserveInventoryResponseDto
        {
            ProductId = item.ProductId,
            Success = true,
            Message = "Inventory reserved successfully.",
            QuantityAvailable = item.QuantityAvailable,
            QuantityReserved = item.QuantityReserved
        };
    }

    private static InventoryResponseDto MapToResponseDto(InventoryItem item)
    {
        return new InventoryResponseDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            QuantityAvailable = item.QuantityAvailable,
            QuantityReserved = item.QuantityReserved
        };
    }
}
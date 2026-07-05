using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryResponseDto>>> GetAll()
    {
        var inventoryItems = await _inventoryService.GetAllAsync();

        return Ok(inventoryItems);
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<InventoryResponseDto>> GetByProductId(string productId)
    {
        var inventoryItem = await _inventoryService.GetByProductIdAsync(productId);

        if (inventoryItem == null)
        {
            return NotFound();
        }

        return Ok(inventoryItem);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryResponseDto>> Create(InventoryCreateDto inventoryDto)
    {
        var inventoryItem = await _inventoryService.CreateAsync(inventoryDto);

        return CreatedAtAction(
            nameof(GetByProductId),
            new { productId = inventoryItem.ProductId },
            inventoryItem);
    }

    [HttpPost("reserve")]
    public async Task<ActionResult<ReserveInventoryResponseDto>> Reserve(ReserveInventoryRequestDto request)
    {
        var result = await _inventoryService.ReserveAsync(request);

        return Ok(result);
    }
}
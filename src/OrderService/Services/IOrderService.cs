using OrderService.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
    Task<List<OrderResponseDto>> GetAllAsync();

    Task<OrderResponseDto?> GetByIdAsync(int id);

    Task<OrderResponseDto> CreateAsync(OrderCreateDto orderDto);
}
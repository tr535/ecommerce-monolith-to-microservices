using EcommerceMonolith.DTOs;

namespace EcommerceMonolith.BLL;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto orderDto);

    Task<OrderResponseDto?> GetOrderByIdAsync(int id);

    Task<List<OrderResponseDto>> GetAllOrdersAsync();
}
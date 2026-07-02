using EcommerceMonolith.BLL;
using EcommerceMonolith.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceMonolith.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(OrderCreateDto orderDto)
    {
        var createdOrder = await _orderService.CreateOrderAsync(orderDto);

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = createdOrder.Id },
            createdOrder
        );
    }
}
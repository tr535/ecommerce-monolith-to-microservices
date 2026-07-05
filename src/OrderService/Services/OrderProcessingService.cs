using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.DAL;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Services;

public class OrderProcessingService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ProductCatalogClient _productCatalogClient;
    private readonly InventoryClient _inventoryClient;

    public OrderProcessingService(
        OrderDbContext context,
        ProductCatalogClient productCatalogClient,
        InventoryClient inventoryClient)
    {
        _context = context;
        _productCatalogClient = productCatalogClient;
        _inventoryClient = inventoryClient;
    }

    public async Task<List<OrderResponseDto>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    public async Task<OrderResponseDto?> GetByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return null;
        }

        return MapToResponseDto(order);
    }

    public async Task<OrderResponseDto> CreateAsync(OrderCreateDto orderDto)
    {
        var order = new Order
        {
            CustomerEmail = orderDto.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>()
        };

        if (orderDto.Items == null || !orderDto.Items.Any())
        {
            order.Status = OrderStatus.Rejected;
            order.TotalAmount = 0;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return MapToResponseDto(order);
        }

        decimal totalAmount = 0;

        foreach (var itemDto in orderDto.Items)
        {
            var product = await _productCatalogClient.GetProductByIdAsync(itemDto.ProductId);

            if (product == null || itemDto.Quantity <= 0)
            {
                order.Status = OrderStatus.Rejected;
                order.TotalAmount = 0;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return MapToResponseDto(order);
            }

            var reserveResult = await _inventoryClient.ReserveAsync(new ReserveInventoryRequestDto
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity
            });

            if (reserveResult == null || !reserveResult.Success)
            {
                order.Status = OrderStatus.Rejected;
                order.TotalAmount = 0;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return MapToResponseDto(order);
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price
            };

            order.Items.Add(orderItem);
            totalAmount += product.Price * itemDto.Quantity;
        }

        order.Status = OrderStatus.Confirmed;
        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    private static OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }
}
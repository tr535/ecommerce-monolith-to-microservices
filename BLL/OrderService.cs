using EcommerceMonolith.DAL;
using EcommerceMonolith.DTOs;
using EcommerceMonolith.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceMonolith.BLL;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto orderDto)
    {
        if (orderDto == null ||
            string.IsNullOrWhiteSpace(orderDto.CustomerEmail) ||
            orderDto.Items == null ||
            !orderDto.Items.Any())
        {
            return await CreateRejectedOrderAsync(orderDto?.CustomerEmail ?? string.Empty);
        }

        var requestedItems = orderDto.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (requestedItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
        {
            return await CreateRejectedOrderAsync(orderDto.CustomerEmail);
        }

        var productIds = requestedItems
            .Select(item => item.ProductId)
            .ToList();

        var products = await _context.Products
            .Include(product => product.InventoryItem)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            return await CreateRejectedOrderAsync(orderDto.CustomerEmail);
        }

        foreach (var requestedItem in requestedItems)
        {
            var product = products.First(product => product.Id == requestedItem.ProductId);

            if (product.InventoryItem == null ||
                product.InventoryItem.QuantityAvailable < requestedItem.Quantity)
            {
                return await CreateRejectedOrderAsync(orderDto.CustomerEmail);
            }
        }

        var order = new Order
        {
            CustomerEmail = orderDto.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Confirmed,
            TotalAmount = 0,
            Items = new List<OrderItem>()
        };

        foreach (var requestedItem in requestedItems)
        {
            var product = products.First(product => product.Id == requestedItem.ProductId);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.Price
            };

            order.Items.Add(orderItem);

            product.InventoryItem!.QuantityAvailable -= requestedItem.Quantity;

            order.TotalAmount += product.Price * requestedItem.Quantity;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == id);

        if (order == null)
        {
            return null;
        }

        return MapToResponseDto(order);
    }

    public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    private async Task<OrderResponseDto> CreateRejectedOrderAsync(string customerEmail)
    {
        var order = new Order
        {
            CustomerEmail = customerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Rejected,
            TotalAmount = 0,
            Items = new List<OrderItem>()
        };

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
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }
}
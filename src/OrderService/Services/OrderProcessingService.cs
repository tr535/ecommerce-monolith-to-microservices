using Microsoft.EntityFrameworkCore;
using MessagingContracts;
using OrderService.Clients;
using OrderService.DAL;
using OrderService.DTOs;
using OrderService.Messaging;
using OrderService.Models;

namespace OrderService.Services;

public class OrderProcessingService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ProductCatalogClient _productCatalogClient;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public OrderProcessingService(
        OrderDbContext context,
        ProductCatalogClient productCatalogClient,
        IRabbitMqPublisher rabbitMqPublisher)
    {
        _context = context;
        _productCatalogClient = productCatalogClient;
        _rabbitMqPublisher = rabbitMqPublisher;
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

        return order == null ? null : MapToResponseDto(order);
    }

    public async Task<OrderResponseDto> CreateAsync(OrderCreateDto orderDto)
    {
        var correlationId = Guid.NewGuid().ToString();

        var order = new Order
        {
            CustomerEmail = orderDto.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 0,
            Items = new List<OrderItem>()
        };

        if (orderDto.Items == null || !orderDto.Items.Any())
        {
            return await SaveRejectedOrderAsync(
                order,
                correlationId,
                "Order must contain at least one item.");
        }

        decimal totalAmount = 0;

        var orderPlacedItems = new List<OrderPlacedItemMessage>();

        foreach (var itemDto in orderDto.Items)
        {
            if (itemDto.Quantity <= 0)
            {
                return await SaveRejectedOrderAsync(
                    order,
                    correlationId,
                    $"Invalid quantity for product {itemDto.ProductId}.");
            }

            var product = await _productCatalogClient.GetProductByIdAsync(itemDto.ProductId);

            if (product == null)
            {
                return await SaveRejectedOrderAsync(
                    order,
                    correlationId,
                    $"Product {itemDto.ProductId} was not found.");
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price
            };

            order.Items.Add(orderItem);

            orderPlacedItems.Add(new OrderPlacedItemMessage
            {
                ProductId = product.Id,
                Quantity = itemDto.Quantity
            });

            totalAmount += product.Price * itemDto.Quantity;
        }

        order.Status = OrderStatus.Pending;
        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderPlacedMessage = new OrderPlacedMessage
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            CorrelationId = correlationId,
            Items = orderPlacedItems
        };

        await _rabbitMqPublisher.PublishAsync(
            orderPlacedMessage,
            RabbitMqNames.OrderPlacedRoutingKey,
            correlationId);

        return MapToResponseDto(order);
    }

    private async Task<OrderResponseDto> SaveRejectedOrderAsync(
        Order order,
        string correlationId,
        string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.TotalAmount = 0;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderRejectedMessage = new OrderRejectedMessage
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            CorrelationId = correlationId,
            Reason = reason
        };

        await _rabbitMqPublisher.PublishAsync(
            orderRejectedMessage,
            RabbitMqNames.OrderRejectedRoutingKey,
            correlationId);

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
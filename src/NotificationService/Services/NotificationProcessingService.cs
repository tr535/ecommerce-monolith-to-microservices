using Microsoft.EntityFrameworkCore;
using NotificationService.DAL;
using NotificationService.DTOs;
using NotificationService.Models;

namespace NotificationService.Services;

public class NotificationProcessingService : INotificationService
{
    private readonly NotificationDbContext _context;

    public NotificationProcessingService(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationResponseDto>> GetAllAsync()
    {
        var notifications = await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToResponseDto).ToList();
    }

    public async Task<NotificationResponseDto?> GetByIdAsync(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null)
        {
            return null;
        }

        return MapToResponseDto(notification);
    }

    public async Task<NotificationResponseDto> CreateAsync(NotificationCreateDto notificationDto)
    {
        var notification = new NotificationMessage
        {
            OrderId = notificationDto.OrderId,
            CustomerEmail = notificationDto.CustomerEmail,
            Status = notificationDto.Status,
            Message = notificationDto.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToResponseDto(notification);
    }

    private static NotificationResponseDto MapToResponseDto(NotificationMessage notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id,
            OrderId = notification.OrderId,
            CustomerEmail = notification.CustomerEmail,
            Status = notification.Status,
            Message = notification.Message,
            CreatedAt = notification.CreatedAt
        };
    }
}
using NotificationService.DTOs;

namespace NotificationService.Services;

public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetAllAsync();

    Task<NotificationResponseDto?> GetByIdAsync(int id);

    Task<NotificationResponseDto> CreateAsync(NotificationCreateDto notificationDto);
}
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponseDto>>> GetAll()
    {
        var notifications = await _notificationService.GetAllAsync();

        return Ok(notifications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationResponseDto>> GetById(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);

        if (notification == null)
        {
            return NotFound();
        }

        return Ok(notification);
    }

    [HttpPost]
    public async Task<ActionResult<NotificationResponseDto>> Create(NotificationCreateDto notificationDto)
    {
        var notification = await _notificationService.CreateAsync(notificationDto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = notification.Id },
            notification);
    }
}
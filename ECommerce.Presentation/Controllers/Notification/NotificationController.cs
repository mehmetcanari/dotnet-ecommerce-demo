using Asp.Versioning;
using ECommerce.Application.Abstract.Service;
using ECommerce.Application.DTO.Request.Notification;
using ECommerce.Application.Services.Notification;
using ECommerce.Application.Validations.Attribute;
using ECommerce.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Presentation.Controllers.Notification;

[ApiController]
[Route("api/v1/notifications")]
[ApiVersion("1.0")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationController(
        INotificationService notificationService, 
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(SendNotificationRequestDto request)
    {
        var result = await _notificationService.TestCreateNotificationAsync(request);
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Notification created" });
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        var result = await _notificationService.GetUserNotificationsAsync(page, size);
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Notifications fetched successfully", data = result.Data });
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var result = await _notificationService.GetUnreadNotificationsAsync();
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Unread notifications fetched successfully", data = result.Data });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _notificationService.GetUnreadCountAsync();
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Unread count fetched successfully", data = result.Data });
    }

    [HttpPost("{id}/mark-read")]
    [ValidateId]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Notification marked as read successfully" });
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync();
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "All notifications marked as read successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Notification deleted successfully" });
    }

    [HttpGet("hub-status")]
    public async Task<IActionResult> GetHubStatus()
    {
        try
        {
            var currentUserResult = await _currentUserService.GetCurrentUserId();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(new { message = "User not found", hubConnected = false });
            }

            var userId = currentUserResult.Data;
            
            var isConnected = NotificationHub.IsUserConnected(userId);
            var connectionId = NotificationHub.GetUserConnectionId(userId);
            var totalConnections = NotificationHub.GetTotalConnections();

            return Ok(new 
            { 
                message = "Hub status retrieved successfully", 
                data = new
                {
                    hubConnected = isConnected,
                    connectionId = connectionId,
                    userId = userId,
                    totalConnections = totalConnections
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error checking hub status: {ex.Message}", hubConnected = false });
        }
    }
} 
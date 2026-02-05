using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;       

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get Notifications for Current User
        /// </summary>
        /// <remarks>
        /// Notification Type: MOOD, TEST, LOCATION, PAIRING, CHAT, SYSTEM (default)
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] NotificationType type = NotificationType.SYSTEM)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId.Value, type.ToString(), pageNumber, pageSize);
                return OkResponse(notifications);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Mark Notification as Read
        /// </summary>
        [HttpPatch("{notificationId:int}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.MarkReadAsync(notificationId, userId.Value);
                return OkResponse(result, "Marked notification as read successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Mark All Notifications as Read
        /// </summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.MarkReadAllAsync(userId.Value);
                return OkResponse(result, "Marked all notifications as read successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Test Create Notification
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNotification()
        {
            try
            {
                var userId = GetCurrentUserId();

                var request = new NotificationRequest
                {
                    Title = "Test Notification",
                    Message = "This is a test notification message.",
                    Type = NotificationType.SYSTEM.ToString()
                };

                await _notificationService.SendNotificationAsync(userId.Value, request);
                return OkResponse();
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Test Push Notification
        /// </summary>
        [HttpPost("push")]
        public async Task<IActionResult> SendNotificationV2([FromQuery] string token)
        {
            try
            {
               
                

                return OkResponse();
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}

using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Test Create Notification
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNotification()
        {
            try
            {
                var request = new NotificationRequest
                {
                    Title = "Test Notification",
                    Message = "This is a test notification message.",
                    UserId = 49,
                    Type = NotificationType.SYSTEM.ToString()
                };

                await _notificationService.SendNotificationAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}

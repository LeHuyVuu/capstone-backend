using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly IDeviceTokenService _deviceTokenService;

        public NotificationController(INotificationService notificationService, IDeviceTokenService deviceTokenService)
        {
            _notificationService = notificationService;
            _deviceTokenService = deviceTokenService;
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
                    UserId = userId.Value,
                    Type = NotificationType.SYSTEM.ToString()
                };

                await _notificationService.SendNotificationAsync(request);
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
               
                await _notificationService.SendNotificationAsyncV2(token);
                return OkResponse();
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Register device for push notifications
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceTokenRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _deviceTokenService.RegisterDeviceTokenAsync(userId.Value, request);
                return OkResponse(result, "Registered device successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}

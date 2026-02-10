using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Notification
{
    public class NotificationRequest
    {
        /// <example>
        /// Test Notification
        /// </example>
        public string Title { get; set; }
        /// <example>
        /// This is a test notification message.
        /// </example>
        public string Message { get; set; }
        /// <example>
        /// SYSTEM
        /// </example>
        public string Type { get; set; }
        /// <example>
        /// 49
        /// </example>
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}

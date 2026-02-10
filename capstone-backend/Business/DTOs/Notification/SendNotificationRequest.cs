namespace capstone_backend.Business.DTOs.Notification
{
    public class SendNotificationRequest
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Data { get; set; } = null;
        public string? ImageUrl { get; set; }
    }
}

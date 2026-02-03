namespace capstone_backend.Business.DTOs.Notification
{
    public class SendNotificationRequest
    {
        public string Token { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> data { get; set; } = null;
        public string ImageUrl { get; set; }
    }
}

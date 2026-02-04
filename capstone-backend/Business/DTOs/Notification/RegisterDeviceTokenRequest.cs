namespace capstone_backend.Business.DTOs.Notification
{
    public class RegisterDeviceTokenRequest
    {
        public string Token { get; set; }
        public string? Platform { get; set; }
    }
}

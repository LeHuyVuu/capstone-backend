namespace capstone_backend.Business.DTOs.Email
{
    public class SendEmailRequest
    {
        public string To { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string? HtmlBody { get; set; } = null;
        public string? TextBody { get; set; } = null;
        public string? FromName { get; set; } = null;
    }
}

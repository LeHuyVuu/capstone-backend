using capstone_backend.Business.DTOs.Email;
using capstone_backend.Business.Interfaces;
using Resend;
using System.Threading;

namespace capstone_backend.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly ILogger<EmailService> _logger;
        private const string DefaultFromEmail = "noreply@couplemood.io.vn";

        public EmailService(IResend resend, ILogger<EmailService> logger)
        {
            _resend = resend;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(SendEmailRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.HtmlBody) && string.IsNullOrWhiteSpace(request.TextBody))
            {
                _logger.LogWarning("Email not sent: both HtmlBody and TextBody are empty.");
                return false;

            }

            try
            {
                var message = new EmailMessage
                {
                    From = string.IsNullOrWhiteSpace(request.FromName)
                        ? DefaultFromEmail
                        : $"{request.FromName} <{DefaultFromEmail}>",
                    To = request.To,
                    Subject = request.Subject,
                    HtmlBody = request.HtmlBody,
                    TextBody = request.TextBody
                };
                var response = await _resend.EmailSendAsync(message, ct);
                _logger.LogInformation("Send successful email");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resend to {To}", request.To);
                return false;
            }
        }
    }
}

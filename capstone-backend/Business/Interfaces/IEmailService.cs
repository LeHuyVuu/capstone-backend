using capstone_backend.Business.DTOs.Email;

namespace capstone_backend.Business.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(SendEmailRequest request, CancellationToken ct = default);
    }
}

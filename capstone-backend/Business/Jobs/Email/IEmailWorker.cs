namespace capstone_backend.Business.Jobs.Email
{
    public interface IEmailWorker
    {
        Task SendCommissionUpdateEmailAsync(string newCommission);
    }
}

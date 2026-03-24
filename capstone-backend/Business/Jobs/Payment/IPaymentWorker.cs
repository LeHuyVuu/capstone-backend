namespace capstone_backend.Business.Jobs.Payment
{
    public interface IPaymentWorker
    {
        /// <summary>
        /// Auto expire pending payments that are older than 15 minutes
        /// </summary>
        Task AutoExpirePendingPaymentsAsync();
    }
}

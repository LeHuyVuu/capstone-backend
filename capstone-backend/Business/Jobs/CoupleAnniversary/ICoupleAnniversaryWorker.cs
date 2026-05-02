namespace capstone_backend.Business.Jobs.CoupleAnniversary
{
    public interface ICoupleAnniversaryWorker
    {
        /// <summary>
        /// Check và gửi notification cho các couple có anniversary date trùng với ngày hiện tại (chỉ check ngày/tháng)
        /// </summary>
        Task SendAnniversaryNotificationsAsync();
    }
}

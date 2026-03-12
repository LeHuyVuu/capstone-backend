namespace capstone_backend.Business.Jobs.Voucher
{
    public interface IVoucherWorker
    {
        Task ActivateVoucherAsync(int voucherId);
        Task EndVoucherAsync(int voucherId);
        Task ExpireVoucherItemAsync(int voucherItemId);
    }
}

namespace capstone_backend.Business.Interfaces
{
    public interface IVoucherItemService
    {
        Task GenerateVoucherItemsAsync(int voucherId, int quantity);
    }
}

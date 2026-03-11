namespace capstone_backend.Business.Interfaces
{
    public interface IVoucherCodeGenerator
    {
        Task<string> GenerateUniqueCodeAsync();
    }
}

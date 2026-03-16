namespace capstone_backend.Business.Interfaces
{
    public interface IQrCodeService
    {
        byte[] GenerateQrWithLogoAsync(string content);
    }
}

namespace capstone_backend.Business.DTOs.Wallet
{
    public class WalletExchangeRateResponse
    {
        public decimal MoneyAmount { get; set; }
        public int PointAmount { get; set; }
        public string Description { get; set; } = null!;
    }
}

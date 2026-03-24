namespace capstone_backend.Business.DTOs.MoneyToPoint
{
    public class ConvertMoneyToPointResponse
    {
        public int TransactionId { get; set; }
        public decimal ConvertedMoney { get; set; }
        public int ConvertedPoints { get; set; }
        public decimal? BalanceBefore { get; set; }
        public decimal? BalanceAfter { get; set; }
        public int PointsBefore { get; set; }
        public int PointsAfter { get; set; }
        public int Rate { get; set; }
    }
}

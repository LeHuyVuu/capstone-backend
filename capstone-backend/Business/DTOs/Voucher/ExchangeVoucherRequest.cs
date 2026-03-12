namespace capstone_backend.Business.DTOs.Voucher
{
    public class ExchangeVoucherRequest
    {
        public List<ExchangeVoucherItemRequest> Items { get; set; } = new();

        /// <example>Đổi voucher cuối tuần</example>
        public string? Note { get; set; }
    }

    public class ExchangeVoucherItemRequest
    {
        public int VoucherId { get; set; }
        public int Quantity { get; set; }
    }
}

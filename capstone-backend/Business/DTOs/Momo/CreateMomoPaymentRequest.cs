namespace capstone_backend.Business.DTOs.Momo
{
    public class CreateMomoPaymentRequest
    {
        public string PartnerCode { get; set; } = null!;
        public string? SubPartnerCode { get; set; }
        public string? StoreName { get; set; }
        public string? StoreId { get; set; }
        public string RequestId { get; set; } = null!;
        public long Amount { get; set; }
        public string OrderId { get; set; } = null!;
        public string OrderInfo { get; set; } = null!;
        public string RedirectUrl { get; set; } = null!;
        public string IpnUrl { get; set; } = null!;
        public string RequestType { get; set; } = null!;
        public string ExtraData { get; set; } = "";
        public List<PaymentItems> Items = new();
        public UserInfo? UserInfo { get; set; }
        public string? ReferenceId { get; set; }
        public string Lang { get; set; } = "vi";
        public string Signature { get; set; } = null!;
    }

    public class PaymentItems
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public string? Manufacturer { get; set; }
        public long Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public long TotalPrice => Price * Quantity;
        public long? TaxAmount { get; set; }
    }

    public class UserInfo
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}

namespace capstone_backend.Business.DTOs.Momo
{
    public class ProcessMemberSubscriptionPaymentRequest
    {
        public int PackageId { get; set; }

        /// <example>MOMO</example>
        public string PaymentMethod { get; set; } = "MOMO";

        /// <example>null</example>
        public string? Description { get; set; }

        /// <example>null</example>
        public string? CouponCode { get; set; }
    }
}

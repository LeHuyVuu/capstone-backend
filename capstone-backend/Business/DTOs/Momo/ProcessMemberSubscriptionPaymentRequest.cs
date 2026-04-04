namespace capstone_backend.Business.DTOs.Momo
{
    public class ProcessMemberSubscriptionPaymentRequest
    {
        public int PackageId { get; set; }

        /// <example>ZALOPAY</example>
        public string PaymentMethod { get; set; } = "ZALOPAY";

        /// <example>null</example>
        public string? Description { get; set; }

        /// <example>null</example>
        public string? CouponCode { get; set; }
    }
}

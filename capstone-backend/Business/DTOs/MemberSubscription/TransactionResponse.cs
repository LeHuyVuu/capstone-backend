namespace capstone_backend.Business.DTOs.MemberSubscription
{
    public class TransactionResponse
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public int UserId { get; set; }
        public string? PaymentMethod { get; set; }
        public string TransType { get; set; } = "MEMBER_SUBSCRIPTION";
        public int DocNo { get; set; }
        public string? Description { get; set; }

        // Momo return
        public string? PayUrl { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? DeeplinkMiniApp { get; set; }

        // Subscription info
        public int? MemberSubscriptionId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
    }
}

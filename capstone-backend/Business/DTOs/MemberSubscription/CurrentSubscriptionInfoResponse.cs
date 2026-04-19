namespace capstone_backend.Business.DTOs.MemberSubscription
{
    public class CurrentSubscriptionInfoResponse
    {
        public bool HasActiveSubscription { get; set; }
        public int? SubscriptionId { get; set; }
        public int? PackageId { get; set; }
        public string? PackageName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
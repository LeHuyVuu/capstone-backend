using capstone_backend.Business.DTOs.SubscriptionPackage;

namespace capstone_backend.Business.DTOs.MemberSubscription
{
    public class MemberSubscriptionResponse
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int PackageId { get; set; }
        public SubscriptionPackageDto? Package { get; set; }
    }
}

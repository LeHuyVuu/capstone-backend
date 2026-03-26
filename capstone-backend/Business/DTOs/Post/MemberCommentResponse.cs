using capstone_backend.Business.DTOs.Accessory;

namespace capstone_backend.Business.DTOs.Post
{
    public class MemberCommentResponse
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? RelationshipStatus { get; set; }
        public List<EquippedAccessoryBriefResponse> EquippedAccessories { get; set; } = new List<EquippedAccessoryBriefResponse>();
    }
}

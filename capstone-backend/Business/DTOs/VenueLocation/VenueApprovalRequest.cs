namespace capstone_backend.Business.DTOs.VenueLocation;

public class VenueApprovalRequest
{
    public int VenueId { get; set; }
    
    /// <summary>
    /// Status to update: "ACTIVE" (Approve) or "DRAFTED" (Reject)
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// Optional reason for rejection
    /// </summary>
    public string? Reason { get; set; }
}

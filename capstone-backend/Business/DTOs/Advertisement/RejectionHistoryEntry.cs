namespace capstone_backend.Business.DTOs.Advertisement;

public class RejectionHistoryEntry
{
    public DateTime RejectedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RejectedBy { get; set; } = "ADMIN SYSTEM"; 
}

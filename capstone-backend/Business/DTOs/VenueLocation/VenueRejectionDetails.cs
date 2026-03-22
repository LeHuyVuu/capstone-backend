using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Model for a single rejection record
/// </summary>
public class RejectionRecord
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = null!;
    
    [JsonPropertyName("rejectedAt")]
    public string RejectedAt { get; set; } = null!;  // ISO 8601 format
    
    [JsonPropertyName("rejectedBy")]
    public string RejectedBy { get; set; } = null!;  // "ADMIN" or admin user ID
}

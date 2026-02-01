namespace capstone_backend.Business.DTOs.VenueLocation;

public class VenueSubmissionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> MissingFields { get; set; } = new();
}

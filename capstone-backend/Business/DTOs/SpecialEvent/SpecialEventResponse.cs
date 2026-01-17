namespace capstone_backend.Business.DTOs.SpecialEvent;

public class SpecialEventResponse
{
    public int Id { get; set; }
    public string? EventName { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

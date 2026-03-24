namespace capstone_backend.Business.DTOs.Report;

public class ReportTypeResponse
{
    public int Id { get; set; }
    public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

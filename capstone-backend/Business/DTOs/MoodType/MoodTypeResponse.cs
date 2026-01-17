namespace capstone_backend.Business.DTOs.MoodType;

public class MoodTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? IconUrl { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

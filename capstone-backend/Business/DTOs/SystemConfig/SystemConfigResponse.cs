namespace capstone_backend.Business.DTOs.SystemConfig
{
    public class SystemConfigResponse
    {
        public int Id { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

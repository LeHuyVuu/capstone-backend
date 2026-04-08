namespace capstone_backend.Business.DTOs.VenueLocation
{
    public class VenueBasicInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}

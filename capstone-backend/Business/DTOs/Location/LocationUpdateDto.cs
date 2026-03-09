namespace capstone_backend.Business.DTOs.Location
{
    public class LocationUpdateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? Heading { get; set; }
        public double? Speed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PartnerLocationDto
    {
        public int PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? Heading { get; set; }
        public double? Speed { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsOnline { get; set; }
    }

    public class LocationSharingStatusDto
    {
        public bool IsEnabled { get; set; }
        public int? CoupleId { get; set; }
        public int? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public bool PartnerIsOnline { get; set; }
    }
}

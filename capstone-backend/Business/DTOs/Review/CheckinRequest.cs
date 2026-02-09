namespace capstone_backend.Business.DTOs.Review
{
    public class CheckinRequest
    {
        public int VenueLocationId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}

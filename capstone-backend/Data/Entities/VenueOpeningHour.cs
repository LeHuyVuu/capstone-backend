using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    [Index("VenueLocationId", "Day", Name = "ix_venue_opening_hours_venue_location_id_day")]
    public partial class VenueOpeningHour
    {
        [Key]
        public int Id { get; set; }
        public int VenueLocationId { get; set; }
        public int Day { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsClosed { get; set; }

        [ForeignKey("VenueLocationId")]
        [InverseProperty("VenueOpeningHours")]
        public virtual VenueLocation VenueLocation { get; set; } = null!;
    }
}

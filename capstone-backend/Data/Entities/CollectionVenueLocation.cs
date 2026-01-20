using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    public partial class CollectionVenueLocation
    {
        [Key]
        public int Id { get; set; }

        public int CollectionId { get; set; }

        public int VenueId { get; set; }

        // Navigation Properties
        [ForeignKey("CollectionId")]
        public virtual Collection Collection { get; set; } = null!;

        [ForeignKey("VenueId")]
        public virtual VenueLocation Venue { get; set; } = null!;
    }
}

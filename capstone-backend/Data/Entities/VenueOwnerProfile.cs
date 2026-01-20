using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class VenueOwnerProfile
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? BusinessName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("venue_owner")]
    public virtual ICollection<Advertisement> advertisements { get; set; } = new List<Advertisement>();

    [ForeignKey("user_id")]
    [InverseProperty("venue_owner_profiles")]
    public virtual UserAccount user { get; set; } = null!;

    [InverseProperty("venue_owner")]
    public virtual ICollection<VenueLocation> venue_locations { get; set; } = new List<VenueLocation>();

    [InverseProperty("venue_owner")]
    public virtual ICollection<Voucher> vouchers { get; set; } = new List<Voucher>();
}

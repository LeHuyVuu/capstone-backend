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

    [InverseProperty("VenueOwner")]
    public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();

    [ForeignKey("UserId")]
    [InverseProperty("VenueOwnerProfiles")]
    public virtual UserAccount User { get; set; } = null!;

    [InverseProperty("VenueOwner")]
    public virtual ICollection<VenueLocation> VenueLocations { get; set; } = new List<VenueLocation>();

    [InverseProperty("VenueOwner")]
    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}

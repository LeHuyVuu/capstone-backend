using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class venue_owner_profile
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    public string? business_name { get; set; }

    public string? phone_number { get; set; }

    public string? email { get; set; }

    public string? address { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("venue_owner")]
    public virtual ICollection<advertisement> advertisements { get; set; } = new List<advertisement>();

    [ForeignKey("user_id")]
    [InverseProperty("venue_owner_profiles")]
    public virtual user_account user { get; set; } = null!;

    [InverseProperty("venue_owner")]
    public virtual ICollection<venue_location> venue_locations { get; set; } = new List<venue_location>();

    [InverseProperty("venue_owner")]
    public virtual ICollection<voucher> vouchers { get; set; } = new List<voucher>();
}

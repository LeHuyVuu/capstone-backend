using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class advertisement
{
    [Key]
    public int id { get; set; }

    public int venue_owner_id { get; set; }

    public string? title { get; set; }

    public string? content { get; set; }

    public string? banner_url { get; set; }

    public string? target_url { get; set; }

    public string? placement_type { get; set; }

    public string? rejection_reason { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("advertisement")]
    public virtual ICollection<AdsOrder> ads_orders { get; set; } = new List<AdsOrder>();

    [InverseProperty("advertisement")]
    public virtual ICollection<venue_location_advertisement> venue_location_advertisements { get; set; } = new List<venue_location_advertisement>();

    [ForeignKey("venue_owner_id")]
    [InverseProperty("advertisements")]
    public virtual venue_owner_profile venue_owner { get; set; } = null!;
}

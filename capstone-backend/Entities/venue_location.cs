using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

[Index("venue_owner_id", Name = "idx_venue_owner")]
[Index("average_rating", Name = "idx_venue_rating")]
public partial class venue_location
{
    [Key]
    public int id { get; set; }

    public int? location_tag_id { get; set; }

    public int venue_owner_id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    public string address { get; set; } = null!;

    public string? email { get; set; }

    public string? phone_number { get; set; }

    public string? website_url { get; set; }

    public DateOnly? opening_time { get; set; }

    public DateOnly? closing_time { get; set; }

    public bool? is_open { get; set; }

    [Precision(18, 2)]
    public decimal? price_min { get; set; }

    [Precision(18, 2)]
    public decimal? price_max { get; set; }

    [Precision(10, 8)]
    public decimal? latitude { get; set; }

    [Precision(11, 8)]
    public decimal? longitude { get; set; }

    [Precision(3, 2)]
    public decimal? average_rating { get; set; }

    [Precision(18, 2)]
    public decimal? avarage_cost { get; set; }

    public int? review_count { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("venue")]
    public virtual ICollection<check_in_history> check_in_histories { get; set; } = new List<check_in_history>();

    [InverseProperty("venue_location")]
    public virtual ICollection<date_plan_item> date_plan_items { get; set; } = new List<date_plan_item>();

    [ForeignKey("location_tag_id")]
    [InverseProperty("venue_locations")]
    public virtual location_tag? location_tag { get; set; }

    [InverseProperty("venue")]
    public virtual ICollection<review> reviews { get; set; } = new List<review>();

    [InverseProperty("venue")]
    public virtual ICollection<venue_location_advertisement> venue_location_advertisements { get; set; } = new List<venue_location_advertisement>();

    [ForeignKey("venue_owner_id")]
    [InverseProperty("venue_locations")]
    public virtual venue_owner_profile venue_owner { get; set; } = null!;

    [InverseProperty("venue")]
    public virtual ICollection<venue_subscription_package> venue_subscription_packages { get; set; } = new List<venue_subscription_package>();

    [ForeignKey("venue_id")]
    [InverseProperty("venues")]
    public virtual ICollection<collection> collections { get; set; } = new List<collection>();
}

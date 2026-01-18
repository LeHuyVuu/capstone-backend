using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("venue_id", Name = "idx_review_venue")]
public partial class review
{
    [Key]
    public int id { get; set; }

    public int venue_id { get; set; }

    public int member_id { get; set; }

    public int? rating { get; set; }

    public string? content { get; set; }

    public DateTime? visited_at { get; set; }

    public bool? is_anonymous { get; set; }

    public int? like_count { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("reviews")]
    public virtual member_profile member { get; set; } = null!;

    [InverseProperty("review")]
    public virtual ICollection<review_like> review_likes { get; set; } = new List<review_like>();

    [ForeignKey("venue_id")]
    [InverseProperty("reviews")]
    public virtual venue_location venue { get; set; } = null!;
}

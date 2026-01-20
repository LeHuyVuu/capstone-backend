using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("VenueId", Name = "idx_review_venue")]
public partial class Review
{
    [Key]
    public int Id { get; set; }

    public int VenueId { get; set; }

    public int MemberId { get; set; }

    public int? Rating { get; set; }

    public string? Content { get; set; }

    public DateTime? VisitedAt { get; set; }

    public bool? IsAnonymous { get; set; }

    public int? LikeCount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("reviews")]
    public virtual MemberProfile member { get; set; } = null!;

    [InverseProperty("review")]
    public virtual ICollection<ReviewLike> review_likes { get; set; } = new List<ReviewLike>();

    [ForeignKey("venue_id")]
    [InverseProperty("reviews")]
    public virtual VenueLocation venue { get; set; } = null!;
}

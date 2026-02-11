using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("ReviewId", "MemberId", Name = "idx_review_member_like")]
public partial class ReviewLike
{
    [Key]
    public int Id { get; set; }

    public int? ReviewId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("ReviewLikes")]
    public virtual MemberProfile? Member { get; set; }

    [ForeignKey("ReviewId")]
    [InverseProperty("ReviewLikes")]
    public virtual Review? Review { get; set; }
}

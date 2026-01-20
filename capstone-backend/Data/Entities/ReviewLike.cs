using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class ReviewLike
{
    [Key]
    public int Id { get; set; }

    public int? ReviewId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("review_likes")]
    public virtual MemberProfile? member { get; set; }

    [ForeignKey("review_id")]
    [InverseProperty("review_likes")]
    public virtual Review? review { get; set; }
}

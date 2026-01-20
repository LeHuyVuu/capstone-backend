using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class CoupleProfileChallenge
{
    [Key]
    public int Id { get; set; }

    public int CoupleId { get; set; }

    public int ChallengeId { get; set; }

    public int? CurrentProgress { get; set; }

    public string? Status { get; set; }

    [Column(TypeName = "jsonb")]
    public string? CompletedMemberIds { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("challenge_id")]
    [InverseProperty("couple_profile_challenges")]
    public virtual Challenge challenge { get; set; } = null!;

    [ForeignKey("couple_id")]
    [InverseProperty("couple_profile_challenges")]
    public virtual CoupleProfile couple { get; set; } = null!;
}

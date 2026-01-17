using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class couple_profile_challenge
{
    [Key]
    public int id { get; set; }

    public int couple_id { get; set; }

    public int challenge_id { get; set; }

    public int? current_progress { get; set; }

    public string? status { get; set; }

    [Column(TypeName = "jsonb")]
    public string? completed_member_ids { get; set; }

    public DateTime? completed_at { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("challenge_id")]
    [InverseProperty("couple_profile_challenges")]
    public virtual challenge challenge { get; set; } = null!;

    [ForeignKey("couple_id")]
    [InverseProperty("couple_profile_challenges")]
    public virtual couple_profile couple { get; set; } = null!;
}

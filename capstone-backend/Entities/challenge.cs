using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class challenge
{
    [Key]
    public int id { get; set; }

    public string? title { get; set; }

    public string? description { get; set; }

    public string challenge_type { get; set; } = null!;

    public int? reward_points { get; set; }

    [Column(TypeName = "jsonb")]
    public string? rule_definition { get; set; }

    public DateTime? start_date { get; set; }

    public DateTime? end_date { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("challenge")]
    public virtual ICollection<couple_profile_challenge> couple_profile_challenges { get; set; } = new List<couple_profile_challenge>();
}

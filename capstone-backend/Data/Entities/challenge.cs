using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Challenge
{
    [Key]
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string ChallengeType { get; set; } = null!;

    public int? RewardPoints { get; set; }

    [Column(TypeName = "jsonb")]
    public string? RuleDefinition { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("challenge")]
    public virtual ICollection<couple_profile_challenge> couple_profile_challenges { get; set; } = new List<couple_profile_challenge>();
}

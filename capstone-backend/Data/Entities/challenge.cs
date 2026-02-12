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

    public string TriggerEvent { get; set; } = null!;

    public int RewardPoints { get; set; } = 0;

    [Column(TypeName = "jsonb")]
    public string? ConditionRules { get; set; }

    public string? GoalMetric { get; set; }

    public int? TargetGoal { get; set; } = 0;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("Challenge")]
    public virtual ICollection<CoupleProfileChallenge> CoupleProfileChallenges { get; set; } = new List<CoupleProfileChallenge>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class date_plan
{
    [Key]
    public int id { get; set; }

    public int couple_id { get; set; }

    public string title { get; set; } = null!;

    public string? note { get; set; }

    public DateTime? planned_start_at { get; set; }

    public DateTime? planned_end_at { get; set; }

    [Precision(18, 2)]
    public decimal? estimated_budget { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("couple_id")]
    [InverseProperty("date_plans")]
    public virtual couple_profile couple { get; set; } = null!;

    [InverseProperty("date_plan")]
    public virtual ICollection<date_plan_item> date_plan_items { get; set; } = new List<date_plan_item>();
}

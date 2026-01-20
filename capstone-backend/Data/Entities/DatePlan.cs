using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class DatePlan
{
    [Key]
    public int Id { get; set; }

    public int CoupleId { get; set; }

    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime? PlannedStartAt { get; set; }

    public DateTime? PlannedEndAt { get; set; }

    [Precision(18, 2)]
    public decimal? EstimatedBudget { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("couple_id")]
    [InverseProperty("date_plans")]
    public virtual CoupleProfile couple { get; set; } = null!;

    [InverseProperty("date_plan")]
    public virtual ICollection<DatePlanItem> date_plan_items { get; set; } = new List<DatePlanItem>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class DatePlanItem
{
    [Key]
    public int Id { get; set; }

    public int DatePlanId { get; set; }

    public int VenueLocationId { get; set; }

    public int? OrderIndex { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("date_plan_id")]
    [InverseProperty("date_plan_items")]
    public virtual DatePlan date_plan { get; set; } = null!;

    [ForeignKey("venue_location_id")]
    [InverseProperty("date_plan_items")]
    public virtual venue_location venue_location { get; set; } = null!;
}

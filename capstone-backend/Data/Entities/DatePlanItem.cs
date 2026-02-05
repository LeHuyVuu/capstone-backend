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

    [ForeignKey("DatePlanId")]
    [InverseProperty("DatePlanItems")]
    public virtual DatePlan DatePlan { get; set; } = null!;

    [ForeignKey("VenueLocationId")]
    [InverseProperty("DatePlanItems")]
    public virtual VenueLocation VenueLocation { get; set; } = null!;
}

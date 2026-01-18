using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class date_plan_item
{
    [Key]
    public int id { get; set; }

    public int date_plan_id { get; set; }

    public int venue_location_id { get; set; }

    public int? order_index { get; set; }

    public TimeOnly? start_time { get; set; }

    public TimeOnly? end_time { get; set; }

    public string? note { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("date_plan_id")]
    [InverseProperty("date_plan_items")]
    public virtual date_plan date_plan { get; set; } = null!;

    [ForeignKey("venue_location_id")]
    [InverseProperty("date_plan_items")]
    public virtual venue_location venue_location { get; set; } = null!;
}

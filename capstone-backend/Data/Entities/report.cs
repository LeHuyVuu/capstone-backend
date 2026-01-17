using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class report
{
    [Key]
    public int id { get; set; }

    public int? reporter_id { get; set; }

    public string? target_type { get; set; }

    public int? target_id { get; set; }

    public string? reason { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("reporter_id")]
    [InverseProperty("reports")]
    public virtual member_profile? reporter { get; set; }
}

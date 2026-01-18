using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class member_mood_log
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public int mood_type_id { get; set; }

    public string? reason { get; set; }

    public string? note { get; set; }

    public string? image_url { get; set; }

    public bool? is_private { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("member_mood_logs")]
    public virtual member_profile member { get; set; } = null!;

    [ForeignKey("mood_type_id")]
    [InverseProperty("member_mood_logs")]
    public virtual mood_type mood_type { get; set; } = null!;
}

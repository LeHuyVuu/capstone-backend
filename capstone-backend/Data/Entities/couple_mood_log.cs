using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class couple_mood_log
{
    [Key]
    public int id { get; set; }

    public int couple_id { get; set; }

    public int couple_mood_type_id { get; set; }

    public string? note { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("couple_id")]
    [InverseProperty("couple_mood_logs")]
    public virtual couple_profile couple { get; set; } = null!;

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("couple_mood_logs")]
    public virtual couple_mood_type couple_mood_type { get; set; } = null!;
}

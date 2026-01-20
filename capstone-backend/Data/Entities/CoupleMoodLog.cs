using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class CoupleMoodLog
{
    [Key]
    public int Id { get; set; }

    public int CoupleId { get; set; }

    public int CoupleMoodTypeId { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("couple_id")]
    [InverseProperty("couple_mood_logs")]
    public virtual couple_profile couple { get; set; } = null!;

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("couple_mood_logs")]
    public virtual couple_mood_type couple_mood_type { get; set; } = null!;
}

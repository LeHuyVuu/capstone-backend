using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class MemberMoodLog
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int MoodTypeId { get; set; }

    public string? Reason { get; set; }

    public string? Note { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsPrivate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("member_mood_logs")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("mood_type_id")]
    [InverseProperty("member_mood_logs")]
    public virtual mood_type mood_type { get; set; } = null!;
}

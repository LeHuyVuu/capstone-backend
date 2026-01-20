using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class mood_type
{
    [Key]
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string? icon_url { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [InverseProperty("mood_type")]
    public virtual ICollection<MemberMoodLog> member_mood_logs { get; set; } = new List<MemberMoodLog>();

    [InverseProperty("mood_types")]
    public virtual ICollection<member_profile> member_profiles { get; set; } = new List<member_profile>();
}

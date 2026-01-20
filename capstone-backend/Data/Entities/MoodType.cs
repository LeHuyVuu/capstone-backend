using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class MoodType
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("MoodType")]
    public virtual ICollection<MemberMoodLog> MemberMoodLogs { get; set; } = new List<MemberMoodLog>();

    [InverseProperty("MoodTypes")]
    public virtual ICollection<MemberProfile> MemberProfiles { get; set; } = new List<MemberProfile>();
}

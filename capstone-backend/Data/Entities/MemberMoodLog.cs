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

    [ForeignKey("MemberId")]
    [InverseProperty("MemberMoodLogs")]
    public virtual MemberProfile Member { get; set; } = null!;

    [ForeignKey("MoodTypeId")]
    [InverseProperty("MemberMoodLogs")]
    public virtual MoodType MoodType { get; set; } = null!;
}

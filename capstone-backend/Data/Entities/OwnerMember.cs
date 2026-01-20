using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[PrimaryKey("OwnerUserId", "MemberUserId")]
[Index("MemberUserId", "Status", Name = "idx_member_status")]
[Index("OwnerUserId", "Status", Name = "idx_owner_status")]
public partial class OwnerMember
{
    [Key]
    public int OwnerUserId { get; set; }

    [Key]
    public int MemberUserId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }
}

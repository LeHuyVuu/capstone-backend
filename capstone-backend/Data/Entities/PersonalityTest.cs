using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class PersonalityTest
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int TestTypeId { get; set; }

    public string? ResultCode { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ResultData { get; set; }

    public string? Status { get; set; }

    public DateTime? TakenAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("PersonalityTests")]
    public virtual MemberProfile Member { get; set; } = null!;

    [ForeignKey("TestTypeId")]
    [InverseProperty("PersonalityTests")]
    public virtual TestType TestType { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class MemberSubscriptionPackage
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int PackageId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("MemberSubscriptionPackages")]
    public virtual MemberProfile Member { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("MemberSubscriptionPackages")]
    public virtual SubscriptionPackage Package { get; set; } = null!;
}

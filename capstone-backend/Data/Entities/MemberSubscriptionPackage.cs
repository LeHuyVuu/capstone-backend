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

    [ForeignKey("member_id")]
    [InverseProperty("member_subscription_packages")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("package_id")]
    [InverseProperty("member_subscription_packages")]
    public virtual subscription_package package { get; set; } = null!;
}

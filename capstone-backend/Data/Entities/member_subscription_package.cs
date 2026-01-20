using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class member_subscription_package
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public int package_id { get; set; }

    public DateTime? start_date { get; set; }

    public DateTime? end_date { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("member_subscription_packages")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("package_id")]
    [InverseProperty("member_subscription_packages")]
    public virtual subscription_package package { get; set; } = null!;
}

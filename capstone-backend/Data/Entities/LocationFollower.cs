using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Bảng quan hệ theo dõi / chia sẻ vị trí giữa users
/// </summary>
[Index("FollowerUserId", "Status", Name = "idx_lf_follower_status")]
[Index("OwnerUserId", "FollowerUserId", Name = "idx_lf_owner_follower")]
[Index("OwnerUserId", "Status", Name = "idx_lf_owner_status")]
[Index("OwnerUserId", "FollowerUserId", Name = "uq_location_followers", IsUnique = true)]
public partial class LocationFollower
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// User trung tâm
    /// </summary>
    public long OwnerUserId { get; set; }

    /// <summary>
    /// User theo dõi
    /// </summary>
    public long FollowerUserId { get; set; }

    /// <summary>
    /// ACTIVE, REMOVED, BLOCKED, PENDING
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; } = null!;

    [StringLength(120)]
    public string? OwnerDisplayName { get; set; }

    public string? OwnerAvatarUrl { get; set; }

    [StringLength(120)]
    public string? FollowerDisplayName { get; set; }

    public string? FollowerAvatarUrl { get; set; }

    [StringLength(20)]
    public string OwnerShareStatus { get; set; } = null!;

    [StringLength(20)]
    public string FollowerShareStatus { get; set; } = null!;

    public bool IsMuted { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? LastSeenAt { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }
}

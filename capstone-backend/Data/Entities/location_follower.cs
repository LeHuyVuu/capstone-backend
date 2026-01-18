using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Bảng quan hệ theo dõi / chia sẻ vị trí giữa users
/// </summary>
[Index("follower_user_id", "status", Name = "idx_lf_follower_status")]
[Index("owner_user_id", "follower_user_id", Name = "idx_lf_owner_follower")]
[Index("owner_user_id", "status", Name = "idx_lf_owner_status")]
[Index("owner_user_id", "follower_user_id", Name = "uq_location_followers", IsUnique = true)]
public partial class location_follower
{
    [Key]
    public long id { get; set; }

    /// <summary>
    /// User trung tâm
    /// </summary>
    public long owner_user_id { get; set; }

    /// <summary>
    /// User theo dõi
    /// </summary>
    public long follower_user_id { get; set; }

    /// <summary>
    /// ACTIVE, REMOVED, BLOCKED, PENDING
    /// </summary>
    [StringLength(20)]
    public string status { get; set; } = null!;

    [StringLength(120)]
    public string? owner_display_name { get; set; }

    public string? owner_avatar_url { get; set; }

    [StringLength(120)]
    public string? follower_display_name { get; set; }

    public string? follower_avatar_url { get; set; }

    [StringLength(20)]
    public string owner_share_status { get; set; } = null!;

    [StringLength(20)]
    public string follower_share_status { get; set; } = null!;

    public bool is_muted { get; set; }

    [StringLength(255)]
    public string? note { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? last_seen_at { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime created_at { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime updated_at { get; set; }
}
